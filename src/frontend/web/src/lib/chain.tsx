"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  createPublicClient,
  createWalletClient,
  custom,
  defineChain,
  http,
  parseSignature,
  toHex,
  type PublicClient,
} from "viem";
import { privateKeyToAccount } from "viem/accounts";
import { api, type EnabledChainConfig, type PaymentAuthorization, type VoucherResponse } from "./api";
import { escrowAbi, ticketAbi, usdcAbi } from "./abis";
import { getEthereumProvider } from "./metamask";

interface ChainState {
  /** Null while loading, and when the backend runs without a chain (simulated minting). */
  config: EnabledChainConfig | null;
  loaded: boolean;
  /** Reads the chain directly from the browser, independent of the TixFlow API. */
  client: PublicClient | null;
  chainName: string;
  txUrl: (hash?: string | null) => string | null;
  addressUrl: (address?: string | null) => string | null;
}

const ChainContext = createContext<ChainState>({
  config: null,
  loaded: false,
  client: null,
  chainName: "",
  txUrl: () => null,
  addressUrl: () => null,
});

export function useChain() {
  return useContext(ChainContext);
}

function nameOf(chainId: number) {
  if (chainId === 8453) return "Base";
  if (chainId === 84532) return "Base Sepolia";
  if (chainId === 31337) return "Local chain (Anvil)";
  return `Chain ${chainId}`;
}

export function ChainProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<EnabledChainConfig | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api
      .getChainConfig()
      .then((c) => setConfig(c.enabled ? c : null))
      .catch(() => {})
      .finally(() => setLoaded(true));
  }, []);

  const value = useMemo<ChainState>(() => {
    const client = config ? (createPublicClient({ transport: http(config.rpcUrl) }) as PublicClient) : null;
    const explorer = config?.explorerUrl ?? null;
    return {
      config,
      loaded,
      client,
      chainName: config ? nameOf(config.chainId) : "",
      txUrl: (hash) => (explorer && hash ? `${explorer}/tx/${hash}` : null),
      addressUrl: (address) => (explorer && address ? `${explorer}/address/${address}` : null),
    };
  }, [config, loaded]);

  // Cache the USDC signing domain while the chain is reachable, so a buyer can still sign a payment
  // (which needs no network at all) during a chain outage.
  useEffect(() => {
    if (value.client && config) usdcDomain(config, value.client).catch(() => {});
  }, [value.client, config]);

  return <ChainContext.Provider value={value}>{children}</ChainContext.Provider>;
}

// ---------------------------------------------------------------------------
// USDC
// ---------------------------------------------------------------------------

interface UsdcDomain {
  name: string;
  version: string;
}

async function usdcDomain(config: EnabledChainConfig, client: PublicClient): Promise<UsdcDomain> {
  const key = `tixflow.usdcDomain.${config.chainId}.${config.contracts.usdc}`;
  try {
    const cached = localStorage.getItem(key);
    if (cached) return JSON.parse(cached);
  } catch {}

  const [name, version] = await Promise.all([
    client.readContract({ address: config.contracts.usdc, abi: usdcAbi, functionName: "name" }),
    client.readContract({ address: config.contracts.usdc, abi: usdcAbi, functionName: "version" }),
  ]);
  const domain = { name, version };
  try {
    localStorage.setItem(key, JSON.stringify(domain));
  } catch {}
  return domain;
}

export const toUsdcUnits = (amount: number) => BigInt(Math.round(amount * 1_000_000));

export function formatUsdcUnits(units: bigint) {
  return (Number(units) / 1_000_000).toLocaleString("en-US", { maximumFractionDigits: 2 });
}

export function usdcBalance(config: EnabledChainConfig, client: PublicClient, address: `0x${string}`) {
  return client.readContract({ address: config.contracts.usdc, abi: usdcAbi, functionName: "balanceOf", args: [address] });
}

/**
 * Signs an EIP-3009 `receiveWithAuthorization` for exactly `amount`, payable only to the escrow contract.
 * Signing is local and free; the backend relays it, so the buyer needs no gas.
 */
// ---------------------------------------------------------------------------
// Writing from the buyer's own wallet
// ---------------------------------------------------------------------------

export interface WalletSigner {
  address: `0x${string}`;
  privateKey?: `0x${string}`;
}

function walletClient(config: EnabledChainConfig, signer: WalletSigner) {
  const chain = defineChain({
    id: config.chainId,
    name: nameOf(config.chainId),
    nativeCurrency: { name: "Ether", symbol: "ETH", decimals: 18 },
    rpcUrls: { default: { http: [config.rpcUrl] } },
  });
  return signer.privateKey
    ? createWalletClient({ account: privateKeyToAccount(signer.privateKey), chain, transport: http(config.rpcUrl) })
    : createWalletClient({ account: signer.address, chain, transport: custom(getEthereumProvider() as never) });
}

export async function signPayment(
  config: EnabledChainConfig,
  _client: PublicClient,
  signer: WalletSigner,
  amount: bigint
): Promise<PaymentAuthorization> {
  const domain = await usdcDomain(config, _client);
  const validBefore = Math.floor(Date.now() / 1000) + 24 * 60 * 60;
  const nonce = toHex(crypto.getRandomValues(new Uint8Array(32)));
  const signature = await walletClient(config, signer).signTypedData({
    domain: { ...domain, chainId: config.chainId, verifyingContract: config.contracts.usdc },
    types: {
      ReceiveWithAuthorization: [
        { name: "from", type: "address" },
        { name: "to", type: "address" },
        { name: "value", type: "uint256" },
        { name: "validAfter", type: "uint256" },
        { name: "validBefore", type: "uint256" },
        { name: "nonce", type: "bytes32" },
      ],
    },
    primaryType: "ReceiveWithAuthorization",
    message: {
      from: signer.address,
      to: config.contracts.eventEscrow,
      value: amount,
      validAfter: 0n,
      validBefore: BigInt(validBefore),
      nonce,
    },
  });

  return { validAfter: 0, validBefore, nonce, signature };
}

/**
 * Mints a confirmed order from the buyer's own wallet using the backend-signed voucher. This is what makes
 * a purchase redeemable even when TixFlow's mint worker is down. The buyer pays the gas.
 */
export async function claimWithVoucher(config: EnabledChainConfig, signer: WalletSigner, v: VoucherResponse) {
  const wallet = walletClient(config, signer);
  const voucher = {
    buyer: v.voucher.buyer,
    tierId: BigInt(v.voucher.tierId),
    quantity: BigInt(v.voucher.quantity),
    nonce: BigInt(v.voucher.nonce),
    deadline: BigInt(v.voucher.deadline),
  };

  if (!v.payment) {
    return wallet.writeContract({
      address: v.ticketContract,
      abi: ticketAbi,
      functionName: "mintWithVoucher",
      args: [voucher, v.signature],
    });
  }

  const sig = parseSignature(v.payment.signature);
  return wallet.writeContract({
    address: v.escrow,
    abi: escrowAbi,
    functionName: "purchaseWithAuthorization",
    args: [
      v.ticketContract,
      voucher,
      v.signature,
      {
        validAfter: BigInt(v.payment.validAfter),
        validBefore: BigInt(v.payment.validBefore),
        nonce: v.payment.nonce,
        v: Number(sig.v ?? (sig.yParity === 0 ? 27n : 28n)),
        r: sig.r,
        s: sig.s,
      },
    ],
  });
}

// ---------------------------------------------------------------------------
// Reading a ticket straight from the chain
// ---------------------------------------------------------------------------

export interface OnChainTicket {
  owner: `0x${string}`;
  redeemed: boolean;
  paid: bigint;
  transferable: boolean;
  image: string;
  status: string;
}

function decodeBase64Utf8(data: string) {
  return new TextDecoder().decode(Uint8Array.from(atob(data), (c) => c.charCodeAt(0)));
}

/** Decodes the fully on-chain `data:application/json;base64,…` token URI. */
export function decodeTokenUri(uri: string): { image: string; status: string; name: string } {
  const json = JSON.parse(decodeBase64Utf8(uri.replace("data:application/json;base64,", "")));
  const status = json.attributes?.find((a: { trait_type: string }) => a.trait_type === "Status")?.value ?? "";
  return { image: json.image, status, name: json.name };
}

export async function readTicket(
  config: EnabledChainConfig,
  client: PublicClient,
  contract: `0x${string}`,
  tokenId: number
): Promise<OnChainTicket> {
  const id = BigInt(tokenId);
  const [owner, redeemed, uri, paid] = await Promise.all([
    client.readContract({ address: contract, abi: ticketAbi, functionName: "ownerOf", args: [id] }),
    client.readContract({ address: contract, abi: ticketAbi, functionName: "isRedeemed", args: [id] }),
    client.readContract({ address: contract, abi: ticketAbi, functionName: "tokenURI", args: [id] }),
    client.readContract({
      address: config.contracts.eventEscrow,
      abi: escrowAbi,
      functionName: "paidFor",
      args: [contract, id],
    }),
  ]);
  const transferable = await client.readContract({
    address: contract,
    abi: ticketAbi,
    functionName: "isTransferable",
    args: [id, owner],
  });
  const meta = decodeTokenUri(uri);
  return { owner, redeemed, paid, transferable, image: meta.image, status: meta.status };
}

export async function readEscrow(config: EnabledChainConfig, client: PublicClient, contract: `0x${string}`) {
  const [registered, cancelled, held, released] = await client.readContract({
    address: config.contracts.eventEscrow,
    abi: escrowAbi,
    functionName: "eventAccount",
    args: [contract],
  });
  return { registered, cancelled, held, released };
}
