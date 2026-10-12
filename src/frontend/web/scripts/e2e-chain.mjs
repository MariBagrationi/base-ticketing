// End-to-end smoke test against a running stack with a chain enabled (e.g. `docker compose up`).
// Plays an admin and a buyer through the API, then verifies every result directly on-chain.
//
//   node scripts/e2e-chain.mjs
//   API_URL=http://localhost:5222 ADMIN_KEY=0x... node scripts/e2e-chain.mjs
//
// ADMIN_KEY must belong to a wallet listed in the API's Auth:AdminWallets. The default is Anvil's public
// development account #3, which the local Docker stack configures as the demo admin. Local use only.
import { createPublicClient, http, parseAbi, toHex } from "viem";
import { generatePrivateKey, privateKeyToAccount } from "viem/accounts";

const API = process.env.API_URL || "http://localhost:5222";
const ADMIN_KEY = process.env.ADMIN_KEY || "0x7c852118294e51e653712a81e05800f419141751be58f605c371e15141b007a6";
const TIMEOUT_MS = Number(process.env.TIMEOUT_MS || 90_000);

const ticketAbi = parseAbi([
  "function ownerOf(uint256) view returns (address)",
  "function isRedeemed(uint256) view returns (bool)",
  "function tokenURI(uint256) view returns (string)",
  "function totalMinted() view returns (uint256)",
  "function redemptionRoot() view returns (bytes32)",
  "function tierPrice(uint256) view returns (uint256)",
  "function isTransferable(uint256,address) view returns (bool)",
]);
const usdcAbi = parseAbi([
  "function name() view returns (string)",
  "function version() view returns (string)",
  "function balanceOf(address) view returns (uint256)",
]);
const escrowAbi = parseAbi([
  "function paidFor(address,uint256) view returns (uint256)",
  "function eventAccount(address) view returns (bool registered, bool cancelled, uint256 held, uint256 released)",
]);

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const step = (text) => console.log(`\n▸ ${text}`);
const ok = (text) => console.log(`  ✓ ${text}`);

function assert(condition, message) {
  if (!condition) throw new Error(`Assertion failed: ${message}`);
  ok(message);
}

async function api(path, { token, method = "GET", body, headers = {} } = {}) {
  const res = await fetch(API + path, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  const json = text ? JSON.parse(text) : {};
  if (!res.ok) {
    const error = new Error(`${method} ${path} -> ${res.status} ${json.message || json.error || text}`);
    error.status = res.status;
    throw error;
  }
  return json;
}

async function waitFor(description, fn) {
  const started = Date.now();
  while (Date.now() - started < TIMEOUT_MS) {
    try {
      const value = await fn();
      if (value) return value;
    } catch (err) {
      if (err.status && err.status !== 404 && err.status !== 409) throw err;
    }
    await sleep(1000);
  }
  throw new Error(`Timed out waiting for: ${description}`);
}

async function signIn(account, chainId) {
  const { nonce } = await api("/auth/nonce");
  const message = [
    "localhost wants you to sign in with your Ethereum account:",
    account.address,
    "",
    "Sign in to TixFlow",
    "",
    "URI: http://localhost",
    "Version: 1",
    `Chain ID: ${chainId}`,
    `Nonce: ${nonce}`,
    `Issued At: ${new Date().toISOString()}`,
  ].join("\n");
  const signature = await account.signMessage({ message });
  const { token } = await api("/auth/verify", { method: "POST", body: { message, signature } });
  return token;
}

function decodeTokenUri(uri) {
  const json = JSON.parse(Buffer.from(uri.replace("data:application/json;base64,", ""), "base64").toString());
  const svg = Buffer.from(json.image.replace("data:image/svg+xml;base64,", ""), "base64").toString();
  return { json, svg };
}

async function main() {
  step("Reading chain configuration from the API");
  const config = await api("/chain/config");
  if (!config.enabled) throw new Error("The API reports no chain. Start the stack with Chain:Enabled=true.");
  const chain = createPublicClient({ transport: http(process.env.RPC_URL || config.rpcUrl) });
  assert(BigInt(await chain.getChainId()) === BigInt(config.chainId), `RPC chain ID matches the API (${config.chainId})`);
  const { eventEscrow: escrow, usdc } = config.contracts;

  const organizer = privateKeyToAccount(ADMIN_KEY);
  const buyer = privateKeyToAccount(generatePrivateKey());
  console.log(`  admin  ${organizer.address}\n  buyer  ${buyer.address}`);

  step("Signing in with Sign-In with Ethereum");
  const organizerToken = await signIn(organizer, config.chainId);
  const buyerToken = await signIn(buyer, config.chainId);
  const adminRoles = (await api("/auth/me", { token: organizerToken })).roles;
  assert(adminRoles.includes("Admin"), "the admin wallet has the Admin role (from Auth:AdminWallets)");
  assert((await api("/auth/me", { token: buyerToken })).roles.join() === "User", "a new wallet gets only the User role");

  step("Role checks are enforced by the server");
  const denied = async (label, call) => {
    try {
      await call();
    } catch (err) {
      return assert(err.status === 403, `${label} is refused with 403`);
    }
    throw new Error(`Assertion failed: ${label} should have been refused`);
  };
  await denied("a buyer creating an event", () => api("/events", { token: buyerToken, method: "POST", body: {} }));
  await denied("a buyer listing users", () => api("/admin/users", { token: buyerToken }));
  await denied("a buyer opening the scanner", () => api("/redeem/events", { token: buyerToken }));

  step("Admin creates an event; the factory deploys its ticket contract as a clone");
  const starts = new Date(Date.now() + 14 * 864e5);
  const created = await api("/events", {
    token: organizerToken,
    method: "POST",
    body: {
      name: `E2E Night ${Date.now() % 100000}`,
      description: "Created by the end-to-end smoke test.",
      venueName: "Test Arena",
      startsAt: starts.toISOString(),
      endsAt: new Date(starts.getTime() + 6 * 36e5).toISOString(),
      tiers: [
        { name: "General Admission", priceUsdc: 25, totalSupply: 100, maxPerWallet: 4 },
        { name: "VIP", priceUsdc: 90, totalSupply: 10, maxPerWallet: 2 },
      ],
    },
  });
  const event = await waitFor("event contract address", async () => {
    const e = await api(`/events/${created.id}`);
    return e.contractAddress ? e : null;
  });
  const ticket = event.contractAddress;
  assert((await chain.getCode({ address: ticket })).length === 2 + 45 * 2, `ticket contract ${ticket} is a 45-byte EIP-1167 clone`);
  const tier = event.tiers.find((t) => t.name === "General Admission");
  assert((await chain.readContract({ address: ticket, abi: ticketAbi, functionName: "tierPrice", args: [BigInt(tier.onChainTierId)] })) === 25_000_000n, "tier price on-chain is 25 USDC");

  step("Buyer joins the fair queue and is admitted");
  await api(`/queue/${event.id}/join`, { token: buyerToken, method: "POST" });
  const { admissionToken } = await waitFor("queue admission", () => api(`/queue/${event.id}/admission`, { token: buyerToken }));
  ok("admission token issued");

  step("Buyer waits for the local faucet (mock USDC)");
  const quantity = 2;
  const total = 25_000_000n * BigInt(quantity);
  await waitFor("faucet funding", async () =>
    (await chain.readContract({ address: usdc, abi: usdcAbi, functionName: "balanceOf", args: [buyer.address] })) >= total);
  const balanceBefore = await chain.readContract({ address: usdc, abi: usdcAbi, functionName: "balanceOf", args: [buyer.address] });
  ok(`buyer holds ${Number(balanceBefore) / 1e6} USDC`);

  step("Buyer reserves and pays with a gasless EIP-3009 signature");
  const { orderId } = await api("/checkout/reserve", {
    token: buyerToken,
    method: "POST",
    headers: { "X-Admission-Token": admissionToken },
    body: { eventId: event.id, tierId: tier.id, quantity },
  });
  const validBefore = BigInt(Math.floor(Date.now() / 1000) + 24 * 3600);
  const paymentNonce = toHex(crypto.getRandomValues(new Uint8Array(32)));
  const paymentSignature = await buyer.signTypedData({
    domain: {
      name: await chain.readContract({ address: usdc, abi: usdcAbi, functionName: "name" }),
      version: await chain.readContract({ address: usdc, abi: usdcAbi, functionName: "version" }),
      chainId: config.chainId,
      verifyingContract: usdc,
    },
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
    message: { from: buyer.address, to: escrow, value: total, validAfter: 0n, validBefore, nonce: paymentNonce },
  });
  const confirmed = await api("/checkout/confirm", {
    token: buyerToken,
    method: "POST",
    body: { orderId, payment: { validAfter: 0, validBefore: Number(validBefore), nonce: paymentNonce, signature: paymentSignature } },
  });
  assert(confirmed.paidOnChain === true, "order confirmed with an on-chain payment authorisation");

  step("Worker relays the purchase: USDC moves into escrow and tickets are minted atomically");
  const tickets = await waitFor("tickets minted", async () => {
    const mine = (await api("/tickets", { token: buyerToken })).filter((t) => t.orderId === orderId);
    return mine.length === quantity && mine.every((t) => t.status === "Minted" && t.tokenId != null) ? mine : null;
  });
  for (const t of tickets) {
    const owner = await chain.readContract({ address: ticket, abi: ticketAbi, functionName: "ownerOf", args: [BigInt(t.tokenId)] });
    assert(owner.toLowerCase() === buyer.address.toLowerCase(), `token #${t.tokenId} is owned by the buyer on-chain`);
    assert((await chain.readContract({ address: escrow, abi: escrowAbi, functionName: "paidFor", args: [ticket, BigInt(t.tokenId)] })) === 25_000_000n, `escrow records 25 USDC paid for token #${t.tokenId}`);
  }
  const balanceAfter = await chain.readContract({ address: usdc, abi: usdcAbi, functionName: "balanceOf", args: [buyer.address] });
  assert(balanceBefore - balanceAfter === total, `buyer paid exactly ${Number(total) / 1e6} USDC`);
  const [, , held] = await chain.readContract({ address: escrow, abi: escrowAbi, functionName: "eventAccount", args: [ticket] });
  assert(held === total, `escrow holds ${Number(held) / 1e6} USDC for the event`);
  assert(tickets.every((t) => /^0x[0-9a-f]{64}$/.test(t.mintTxHash)), "the API exposes the mint transaction hash");

  step("The ticket is transfer-locked and its artwork lives fully on-chain");
  const first = tickets[0];
  assert((await chain.readContract({ address: ticket, abi: ticketAbi, functionName: "isTransferable", args: [BigInt(first.tokenId), buyer.address] })) === false, "the buyer cannot transfer the ticket before the unlock time");
  let meta = decodeTokenUri(await chain.readContract({ address: ticket, abi: ticketAbi, functionName: "tokenURI", args: [BigInt(first.tokenId)] }));
  assert(meta.json.attributes.some((a) => a.trait_type === "Status" && a.value === "Valid"), "tokenURI reports status Valid");
  assert(meta.svg.includes("<svg") && meta.svg.includes("General Admission"), "tokenURI embeds an SVG naming the tier");

  step("The mint voucher lets the buyer prove the purchase on-chain without the worker");
  const voucher = await api(`/orders/${orderId}/voucher`, { token: buyerToken });
  assert(voucher.ticketContract.toLowerCase() === ticket.toLowerCase() && voucher.voucher.quantity === String(quantity), "voucher covers the order");

  step("Door: staff scan the ticket and the holder signs a one-time challenge");
  await denied("the buyer scanning their own ticket", () => api(`/redeem/${first.id}/challenge`, { token: buyerToken, method: "POST" }));
  const { challenge } = await api(`/redeem/${first.id}/challenge`, { token: organizerToken, method: "POST" });
  const redeemed = await api(`/redeem/${first.id}/confirm`, {
    token: organizerToken,
    method: "POST",
    body: { challenge, signature: await buyer.signMessage({ message: challenge }) },
  });
  assert(redeemed.success === true, "ticket admitted at the door (off-chain, instant)");

  step("Redemption is checkpointed on-chain as a Merkle root and the ticket becomes a soulbound badge");
  await waitFor("badge claimed on-chain", () =>
    chain.readContract({ address: ticket, abi: ticketAbi, functionName: "isRedeemed", args: [BigInt(first.tokenId)] }));
  ok(`isRedeemed(#${first.tokenId}) is true on-chain`);
  const root = await chain.readContract({ address: ticket, abi: ticketAbi, functionName: "redemptionRoot" });
  assert(/^0x[0-9a-f]{64}$/.test(root) && BigInt(root) !== 0n, `redemption Merkle root published: ${root.slice(0, 18)}…`);
  meta = decodeTokenUri(await chain.readContract({ address: ticket, abi: ticketAbi, functionName: "tokenURI", args: [BigInt(first.tokenId)] }));
  assert(meta.json.attributes.some((a) => a.value === "Attended") && meta.svg.includes("ATTENDED"), "tokenURI now renders the attendance badge");
  const proof = await api(`/tickets/${first.id}/attendance-proof`, { token: buyerToken });
  assert(proof.root === root, "the API's Merkle proof targets the root that is live on-chain");

  step("The indexer recorded the on-chain history");
  const activity = await waitFor("indexed activity", async () => {
    const feed = await api(`/events/${event.id}/activity`);
    return feed.some((a) => a.kind === "BadgeClaimed") ? feed : null;
  });
  const kinds = new Set(activity.map((a) => a.kind));
  for (const kind of ["EventCreated", "PaymentDeposited", "Minted", "RedemptionsCheckpointed", "BadgeClaimed"]) {
    assert(kinds.has(kind), `activity feed contains ${kind}`);
  }

  console.log("\nAll end-to-end checks passed.\n");
}

main().catch((err) => {
  console.error(`\n✗ ${err.message}\n`);
  process.exit(1);
});
