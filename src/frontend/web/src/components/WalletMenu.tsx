"use client";

import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/lib/auth";
import { shortAddress } from "@/lib/format";
import { Avatar, Button, CheckIcon, Spinner, inputClass } from "./ui";
import { useToast } from "./Toaster";

export function WalletMenu() {
  const { wallet, wallets, status, error, createWallet, switchWallet, removeWallet, retry } = useAuth();
  const toast = useToast();
  const [open, setOpen] = useState(false);
  const [naming, setNaming] = useState(false);
  const [name, setName] = useState("");
  const rootRef = useRef<HTMLDivElement>(null);

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

  if (status === "loading") {
    return <div className="h-9 w-36 animate-pulse rounded-xl bg-surface-alt" />;
  }

  if (!wallet) {
    return (
      <Button
        size="sm"
        onClick={() => {
          const w = createWallet();
          toast({ tone: "success", title: `Demo wallet “${w.label}” created`, description: "You're signed in. No extension needed." });
        }}
      >
        Create demo wallet
      </Button>
    );
  }

  const others = wallets.filter((w) => w.address !== wallet.address);

  const submitNew = (e: React.FormEvent) => {
    e.preventDefault();
    const w = createWallet(name);
    setName("");
    setNaming(false);
    setOpen(false);
    toast({ tone: "success", title: `Switched to new wallet “${w.label}”` });
  };

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
        {status === "signing-in" ? (
          <Spinner className="h-3.5 w-3.5 text-muted" />
        ) : status === "error" ? (
          <span className="h-2 w-2 rounded-full bg-danger" title="Sign-in failed" />
        ) : (
          <span className="hidden font-mono text-xs text-muted sm:inline">{shortAddress(wallet.address)}</span>
        )}
      </button>

      {open && (
        <div
          role="menu"
          className="animate-rise absolute right-0 top-11 z-50 w-[min(20rem,calc(100vw-2rem))] overflow-hidden rounded-2xl border border-border bg-surface shadow-pop"
        >
          <div className="border-b border-border p-4">
            <div className="flex items-center gap-3">
              <Avatar address={wallet.address} size={40} />
              <div className="min-w-0">
                <p className="font-semibold">{wallet.label}</p>
                <button
                  className="font-mono text-xs text-muted hover:text-foreground"
                  onClick={() => {
                    navigator.clipboard?.writeText(wallet.address);
                    toast({ tone: "info", title: "Address copied" });
                  }}
                  title="Copy address"
                >
                  {shortAddress(wallet.address)} · copy
                </button>
              </div>
            </div>
            {status === "error" && (
              <div className="mt-3 rounded-lg bg-danger-soft p-2.5 text-xs text-danger">
                {error}
                <button className="ml-1 font-semibold underline" onClick={retry}>
                  Retry
                </button>
              </div>
            )}
          </div>

          {others.length > 0 && (
            <div className="border-b border-border p-2">
              <p className="px-2 pb-1 pt-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">Switch wallet</p>
              {others.map((w) => (
                <button
                  key={w.address}
                  role="menuitem"
                  onClick={() => {
                    switchWallet(w.address);
                    setOpen(false);
                  }}
                  className="flex w-full items-center gap-3 rounded-lg px-2 py-2 text-left text-sm hover:bg-surface-alt"
                >
                  <Avatar address={w.address} size={24} />
                  <span className="flex-1 font-medium">{w.label}</span>
                  <span className="font-mono text-xs text-muted">{shortAddress(w.address)}</span>
                </button>
              ))}
            </div>
          )}

          <div className="p-2">
            {naming ? (
              <form onSubmit={submitNew} className="flex gap-2 p-1">
                <input
                  id="new-wallet-name"
                  autoFocus
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Wallet name, e.g. Venue staff"
                  className={`${inputClass} py-1.5`}
                />
                <Button size="sm" type="submit">
                  <CheckIcon className="h-4 w-4" />
                </Button>
              </form>
            ) : (
              <button
                role="menuitem"
                onClick={() => setNaming(true)}
                className="w-full rounded-lg px-2 py-2 text-left text-sm font-medium text-accent hover:bg-surface-alt"
              >
                + New demo wallet
              </button>
            )}
            <button
              role="menuitem"
              onClick={() => {
                if (confirm(`Remove “${wallet.label}”? Its tickets stay on the server but you'll lose access to them.`)) {
                  removeWallet(wallet.address);
                  setOpen(false);
                }
              }}
              className="w-full rounded-lg px-2 py-2 text-left text-sm text-muted hover:bg-surface-alt hover:text-danger"
            >
              Remove this wallet
            </button>
          </div>

          <p className="border-t border-border bg-surface-alt px-4 py-2.5 text-[11px] leading-relaxed text-muted">
            Demo wallets live in this browser only. Never send real funds to them.
          </p>
        </div>
      )}
    </div>
  );
}
