"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { privateKeyToAccount } from "viem/accounts";
import { api, setReauthenticate, setToken, type Role } from "./api";
import { requestMetaMaskAccounts, signMetaMaskMessage } from "./metamask";

export interface DemoWallet {
  label: string;
  address: `0x${string}`;
  privateKey?: `0x${string}`;
  source: "metamask" | "demo";
}

export type AuthStatus = "loading" | "no-wallet" | "signing-in" | "ready" | "error";

interface AuthState {
  wallets: DemoWallet[];
  wallet: DemoWallet | null;
  userId: string | null;
  roles: Role[];
  staffEventIds: string[];
  isAdmin: boolean;
  isStaff: boolean;
  isBuyer: boolean;
  status: AuthStatus;
  isAuthed: boolean;
  error: string | null;
  connectMetaMask: () => Promise<DemoWallet>;
  importWallet: (label: string, privateKey: `0x${string}`) => DemoWallet;
  switchWallet: (address: string) => void;
  removeWallet: (address: string) => void;
  signMessage: (message: string) => Promise<string>;
  tokenFor: (address: string) => string | null;
  refreshRoles: () => Promise<void>;
  retry: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}

const WALLETS_KEY = "tixflow.wallets.v2";
const ACTIVE_KEY = "tixflow.activeWallet.v2";
const jwtKey = (address: string) => `tixflow.jwt.${address.toLowerCase()}`;

function read(key: string) {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string | null) {
  try {
    if (value === null) localStorage.removeItem(key);
    else localStorage.setItem(key, value);
  } catch {}
}

async function signIn(wallet: DemoWallet): Promise<string> {
  const { nonce } = await api.getNonce();
  const chain = await api.getChainConfig().catch(() => null);
  const chainId = chain?.enabled ? chain.chainId : 8453;
  const message = [
    `${window.location.host} wants you to sign in with your Ethereum account:`,
    wallet.address,
    "",
    "Sign in to TixFlow",
    "",
    `URI: ${window.location.origin}`,
    "Version: 1",
    `Chain ID: ${chainId}`,
    `Nonce: ${nonce}`,
    `Issued At: ${new Date().toISOString()}`,
  ].join("\n");
  const signature = wallet.privateKey
    ? await privateKeyToAccount(wallet.privateKey).signMessage({ message })
    : await signMetaMaskMessage(wallet.address, message);
  const { token } = await api.verify(message, signature);
  write(jwtKey(wallet.address), token);
  return token;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [wallets, setWallets] = useState<DemoWallet[]>([]);
  const [activeAddress, setActiveAddress] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [userId, setUserId] = useState<string | null>(null);
  const [roles, setRoles] = useState<Role[]>([]);
  const [staffEventIds, setStaffEventIds] = useState<string[]>([]);
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);
  const walletsRef = useRef<DemoWallet[]>([]);

  useEffect(() => {
    let stored: DemoWallet[] = [];
    try {
      stored = (JSON.parse(read(WALLETS_KEY) || "[]") as DemoWallet[]).filter((w) => w.source === "demo");
    } catch {}
    walletsRef.current = stored;
    setWallets(stored);
    const active = read(ACTIVE_KEY);
    setActiveAddress(stored.find((w) => w.address.toLowerCase() === active?.toLowerCase())?.address ?? null);
    setLoaded(true);
  }, []);

  const persist = useCallback((next: DemoWallet[], active: string | null) => {
    walletsRef.current = next;
    setWallets(next);
    setActiveAddress(active);
    write(WALLETS_KEY, JSON.stringify(next));
    write(ACTIVE_KEY, active);
  }, []);

  const wallet = useMemo(
    () => wallets.find((w) => w.address.toLowerCase() === activeAddress?.toLowerCase()) ?? null,
    [wallets, activeAddress]
  );

  useEffect(() => {
    if (!loaded) return;
    if (!wallet) {
      setToken(null);
      setReauthenticate(null);
      setUserId(null);
      setRoles([]);
      setStaffEventIds([]);
      setStatus("no-wallet");
      return;
    }

    let cancelled = false;
    setReauthenticate(async () => {
      try {
        const token = await signIn(wallet);
        if (!cancelled) setToken(token);
        return token;
      } catch {
        return null;
      }
    });

    (async () => {
      setStatus("signing-in");
      setError(null);
      setUserId(null);
      setRoles([]);
      try {
        setToken(read(jwtKey(wallet.address)) ?? (await signIn(wallet)));
        if (cancelled) return;
        const me = await api.getMe();
        if (cancelled) return;
        setUserId(me.userId);
        setRoles(me.roles);
        setStaffEventIds(me.staffEventIds);
        setStatus("ready");
      } catch (err) {
        if (cancelled) return;
        setToken(null);
        setError(err instanceof Error ? err.message : "Sign-in failed");
        setStatus("error");
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [wallet, loaded, attempt]);

  const connectMetaMask = useCallback(async () => {
    const [address] = await requestMetaMaskAccounts();
    const normalized = address.toLowerCase() as `0x${string}`;
    const connected: DemoWallet = { label: "MetaMask", address: normalized, source: "metamask" };
    const next = [...walletsRef.current.filter((w) => w.address.toLowerCase() !== normalized), connected];
    persist(next, normalized);
    return connected;
  }, [persist]);

  const importWallet = useCallback(
    (label: string, privateKey: `0x${string}`) => {
      const address = privateKeyToAccount(privateKey).address;
      const existing = walletsRef.current.find((w) => w.address.toLowerCase() === address.toLowerCase());
      if (existing) {
        persist(walletsRef.current, address);
        return existing;
      }
      const imported: DemoWallet = { label, address, privateKey, source: "demo" };
      persist([...walletsRef.current, imported], address);
      return imported;
    },
    [persist]
  );

  const switchWallet = useCallback(
    (address: string) => {
      if (walletsRef.current.some((w) => w.address.toLowerCase() === address.toLowerCase())) persist(walletsRef.current, address);
    },
    [persist]
  );

  const removeWallet = useCallback(
    (address: string) => {
      const next = walletsRef.current.filter((w) => w.address.toLowerCase() !== address.toLowerCase());
      write(jwtKey(address), null);
      persist(next, activeAddress?.toLowerCase() === address.toLowerCase() ? null : activeAddress);
    },
    [persist, activeAddress]
  );

  const signMessage = useCallback(
    async (message: string) => {
      if (!wallet) throw new Error("Connect a wallet first.");
      return wallet.privateKey
        ? privateKeyToAccount(wallet.privateKey).signMessage({ message })
        : signMetaMaskMessage(wallet.address, message);
    },
    [wallet]
  );

  const tokenFor = useCallback((address: string) => read(jwtKey(address)), []);
  const refreshRoles = useCallback(async () => {
    const me = await api.getMe();
    setRoles(me.roles);
    setStaffEventIds(me.staffEventIds);
  }, []);
  const retry = useCallback(() => setAttempt((n) => n + 1), []);
  const isAdmin = roles.includes("Admin");
  const value: AuthState = {
    wallets,
    wallet,
    userId,
    roles,
    staffEventIds,
    isAdmin,
    isStaff: roles.includes("Staff"),
    isBuyer: roles.includes("User") && !isAdmin,
    status,
    isAuthed: status === "ready",
    error,
    connectMetaMask,
    importWallet,
    switchWallet,
    removeWallet,
    signMessage,
    tokenFor,
    refreshRoles,
    retry,
  };

  useEffect(() => {
    if (typeof window === "undefined" || !window.ethereum) return;
    const provider = window.ethereum;
    const onAccountsChanged = (...args: unknown[]) => {
      const addresses = args[0];
      if (Array.isArray(addresses) && typeof addresses[0] === "string") {
        const current = walletsRef.current.find((w) => w.source === "metamask");
        if (current) {
          const next = { ...current, address: addresses[0].toLowerCase() as `0x${string}` };
          persist([...walletsRef.current.filter((w) => w.source !== "metamask"), next], next.address);
        }
      }
    };
    provider.on?.("accountsChanged", onAccountsChanged);
    return () => provider.removeListener?.("accountsChanged", onAccountsChanged);
  }, [persist]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
