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
import { generatePrivateKey, privateKeyToAccount } from "viem/accounts";
import { api, setReauthenticate, setToken, type Role } from "./api";

export interface DemoWallet {
  label: string;
  address: `0x${string}`;
  privateKey: `0x${string}`;
}

export type AuthStatus = "loading" | "no-wallet" | "signing-in" | "ready" | "error";

interface AuthState {
  wallets: DemoWallet[];
  wallet: DemoWallet | null;
  userId: string | null;
  /** Live roles from the server. They only drive what the UI shows; the API enforces them itself. */
  roles: Role[];
  staffEventIds: string[];
  isAdmin: boolean;
  /** Staff or Admin: may use the door scanner. */
  isStaff: boolean;
  /** User or Admin: may queue, buy and hold tickets. */
  isBuyer: boolean;
  status: AuthStatus;
  isAuthed: boolean;
  error: string | null;
  createWallet: (label?: string) => DemoWallet;
  /** Adds a wallet from an existing private key (demo accounts) and switches to it. */
  importWallet: (label: string, privateKey: `0x${string}`) => DemoWallet;
  switchWallet: (address: string) => void;
  removeWallet: (address: string) => void;
  signMessage: (message: string) => Promise<string>;
  /** Cached session token of any local wallet, if it has signed in before. */
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

const WALLETS_KEY = "tixflow.demoWallets";
const ACTIVE_KEY = "tixflow.activeWallet";
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

function defaultLabel(count: number) {
  return count === 0 ? "Buyer" : `Wallet ${count + 1}`;
}

async function signIn(wallet: DemoWallet): Promise<string> {
  const account = privateKeyToAccount(wallet.privateKey);
  const { nonce } = await api.getNonce();
  // Sign in on the chain the backend actually uses; Base mainnet when it runs without one.
  const chain = await api.getChainConfig().catch(() => null);
  const chainId = chain?.enabled ? chain.chainId : 8453;
  const message = [
    `${window.location.host} wants you to sign in with your Ethereum account:`,
    account.address,
    "",
    "Sign in to TixFlow",
    "",
    `URI: ${window.location.origin}`,
    "Version: 1",
    `Chain ID: ${chainId}`,
    `Nonce: ${nonce}`,
    `Issued At: ${new Date().toISOString()}`,
  ].join("\n");
  const signature = await account.signMessage({ message });
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
      stored = JSON.parse(read(WALLETS_KEY) || "[]");
    } catch {}
    const active = read(ACTIVE_KEY);
    walletsRef.current = stored;
    setWallets(stored);
    setActiveAddress(stored.find((w) => w.address === active)?.address ?? stored[0]?.address ?? null);
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
    () => wallets.find((w) => w.address === activeAddress) ?? null,
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
        const cached = read(jwtKey(wallet.address));
        setToken(cached ?? (await signIn(wallet)));
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

  const createWallet = useCallback(
    (label?: string) => {
      const privateKey = generatePrivateKey();
      const created: DemoWallet = {
        label: label?.trim() || defaultLabel(walletsRef.current.length),
        address: privateKeyToAccount(privateKey).address,
        privateKey,
      };
      persist([...walletsRef.current, created], created.address);
      return created;
    },
    [persist]
  );

  const importWallet = useCallback(
    (label: string, privateKey: `0x${string}`) => {
      const address = privateKeyToAccount(privateKey).address;
      const existing = walletsRef.current.find((w) => w.address === address);
      if (existing) {
        persist(walletsRef.current, address);
        return existing;
      }
      const imported: DemoWallet = { label, address, privateKey };
      persist([...walletsRef.current, imported], address);
      return imported;
    },
    [persist]
  );

  const switchWallet = useCallback(
    (address: string) => {
      if (walletsRef.current.some((w) => w.address === address)) persist(walletsRef.current, address);
    },
    [persist]
  );

  const removeWallet = useCallback(
    (address: string) => {
      const next = walletsRef.current.filter((w) => w.address !== address);
      write(jwtKey(address), null);
      persist(next, activeAddress === address ? (next[0]?.address ?? null) : activeAddress);
    },
    [persist, activeAddress]
  );

  const signMessage = useCallback(
    async (message: string) => {
      if (!wallet) throw new Error("Create a demo wallet first.");
      return privateKeyToAccount(wallet.privateKey).signMessage({ message });
    },
    [wallet]
  );

  const refreshRoles = useCallback(async () => {
    const me = await api.getMe();
    setRoles(me.roles);
    setStaffEventIds(me.staffEventIds);
  }, []);

  const tokenFor = useCallback((address: string) => read(jwtKey(address)), []);
  const retry = useCallback(() => setAttempt((n) => n + 1), []);

  const isAdmin = roles.includes("Admin");
  const value: AuthState = {
    wallets,
    wallet,
    userId,
    roles,
    staffEventIds,
    isAdmin,
    isStaff: isAdmin || roles.includes("Staff"),
    isBuyer: isAdmin || roles.includes("User"),
    status,
    isAuthed: status === "ready",
    error,
    createWallet,
    importWallet,
    switchWallet,
    removeWallet,
    signMessage,
    tokenFor,
    refreshRoles,
    retry,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
