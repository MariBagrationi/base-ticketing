"use client";

import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/lib/auth";
import { formatUsdcUnits, usdcBalance, useChain } from "@/lib/chain";
import { shortAddress } from "@/lib/format";
import { Avatar, Button, Spinner } from "./ui";
import { useToast } from "./Toaster";
import { DemoAccounts, RoleBadge } from "./RoleGate";

export function WalletMenu() {
  const { wallet, wallets, roles, status, error, connectMetaMask, switchWallet, removeWallet, retry } = useAuth();
  const toast = useToast();
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const { config, client } = useChain();
  const [balance, setBalance] = useState<bigint | null>(null);

  useEffect(() => {
    if (!open || !config || !client || !wallet) return;
    let cancelled = false;
    setBalance(null);
    usdcBalance(config, client, wallet.address)
      .then((b) => !cancelled && setBalance(b))
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [open, config, client, wallet]);

  useEffect(() => {
    if (!open) return;
    const onClick = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && setOpen(false);
    document.addEventListener("mousedown", onClick);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onClick);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  if (status === "loading") return <div className="h-9 w-36 animate-pulse rounded-xl bg-surface-alt" />;

  if (!wallet) {
    return (
      <div className="flex items-center gap-2">
        <DemoAccounts />
        <Button
          size="sm"
          onClick={async () => {
            try {
              await connectMetaMask();
            } catch (err) {
              toast({ tone: "error", title: "MetaMask connection failed", description: err instanceof Error ? err.message : undefined });
            }
          }}
        >
          Connect MetaMask
        </Button>
      </div>
    );
  }

  const others = wallets.filter((w) => w.address.toLowerCase() !== wallet.address.toLowerCase());

  return (
    <div ref={rootRef} className="relative">
      <button
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
        aria-haspopup="menu"
        className="flex h-9 items-center gap-2 rounded-xl border border-border bg-surface pl-1.5 pr-3 text-sm transition-colors hover:border-border-strong"
      >
        <Avatar address={wallet.address} size={24} />
        <span className="font-medium">{wallet.label}</span>
        {status === "signing-in" ? <Spinner className="h-3.5 w-3.5 text-muted" /> : status === "error" ? <span className="h-2 w-2 rounded-full bg-danger" title="Sign-in failed" /> : <span className="hidden font-mono text-xs text-muted sm:inline">{shortAddress(wallet.address)}</span>}
      </button>

      {open && (
        <div role="menu" className="animate-rise absolute right-0 top-11 z-50 w-[min(20rem,calc(100vw-2rem))] overflow-hidden rounded-2xl border border-border bg-surface shadow-pop">
          <div className="border-b border-border p-4">
            <div className="flex items-center gap-3">
              <Avatar address={wallet.address} size={40} />
              <div className="min-w-0">
                <p className="font-semibold">{wallet.label}</p>
                <button className="font-mono text-xs text-muted hover:text-foreground" onClick={() => { navigator.clipboard?.writeText(wallet.address); toast({ tone: "info", title: "Address copied" }); }} title="Copy address">
                  {shortAddress(wallet.address)} · copy
                </button>
                {config && <p className="mt-0.5 text-xs tabular-nums text-muted">{balance === null ? "reading balance…" : `${formatUsdcUnits(balance)} USDC on-chain`}</p>}
              </div>
            </div>
            {status === "ready" && <p className="mt-3 flex flex-wrap items-center gap-1.5 text-xs text-muted">Roles: {roles.length === 0 ? "none" : roles.map((r) => <RoleBadge key={r} role={r} />)}</p>}
            {status === "error" && <div className="mt-3 rounded-lg bg-danger-soft p-2.5 text-xs text-danger">{error}<button className="ml-1 font-semibold underline" onClick={retry}>Retry</button></div>}
          </div>

          {others.length > 0 && (
            <div className="border-b border-border p-2">
              <p className="px-2 pb-1 pt-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">Switch wallet</p>
              {others.map((w) => <button key={w.address} role="menuitem" onClick={() => { switchWallet(w.address); setOpen(false); }} className="flex w-full items-center gap-3 rounded-lg px-2 py-2 text-left text-sm hover:bg-surface-alt"><Avatar address={w.address} size={24} /><span className="flex-1 font-medium">{w.label}</span><span className="font-mono text-xs text-muted">{shortAddress(w.address)}</span></button>)}
            </div>
          )}

          <div className="p-2">
            <button role="menuitem" onClick={async () => { try { await connectMetaMask(); setOpen(false); } catch (err) { toast({ tone: "error", title: "MetaMask connection failed", description: err instanceof Error ? err.message : undefined }); } }} className="w-full rounded-lg px-2 py-2 text-left text-sm font-medium text-accent hover:bg-surface-alt">
              Connect or switch MetaMask
            </button>
            <button role="menuitem" onClick={() => { removeWallet(wallet.address); setOpen(false); }} className="w-full rounded-lg px-2 py-2 text-left text-sm text-muted hover:bg-surface-alt hover:text-danger">
              Sign out this wallet
            </button>
          </div>

          <DemoAccounts className="border-t border-border p-3" />
          <p className="border-t border-border bg-surface-alt px-4 py-2.5 text-[11px] leading-relaxed text-muted">Connect MetaMask for buyer accounts. Local Admin and Staff accounts are available only in demo mode.</p>
        </div>
      )}
    </div>
  );
}
