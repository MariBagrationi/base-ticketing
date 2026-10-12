"use client";

import { useEffect, useState, type ReactNode } from "react";
import { api, type DemoAccount, type Role } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { Badge, Button, Notice, PageSpinner } from "./ui";
import { useToast } from "./Toaster";

type Need = "buyer" | "staff" | "admin";

const copy: Record<Need, { title: string; body: string; roles: Role[] }> = {
  buyer: {
    title: "This area is for ticket buyers",
    body: "Queueing, checkout and tickets belong to wallets with the User role. Door staff accounts can't buy tickets.",
    roles: ["User"],
  },
  staff: {
    title: "The door scanner is for venue staff",
    body: "Only wallets with the Staff role can scan and check in tickets. Staff can scan tickets from every active event.",
    roles: ["Staff"],
  },
  admin: {
    title: "This area is for administrators",
    body: "Creating and cancelling events, managing roles and viewing the audit log need the Admin role, which is set in the server's configuration.",
    roles: ["Admin"],
  },
};

/**
 * Hides a page from wallets without the required role. This is only a courtesy to the user:
 * every API endpoint behind these pages checks the role again on the server.
 */
export function RoleGate({ need, children }: { need: Need; children: ReactNode }) {
  const { status, error, retry, isAdmin, isStaff, isBuyer, roles, wallet } = useAuth();

  if (status === "loading" || status === "signing-in") return <PageSpinner />;

  const allowed = need === "admin" ? isAdmin : need === "staff" ? isStaff : isBuyer;
  if (status === "ready" && allowed) return <>{children}</>;
  if (status === "no-wallet" && need === "buyer") return <>{children}</>;

  const info = copy[need];
  return (
    <div className="mx-auto max-w-2xl px-4 py-14 sm:px-6">
      {status === "error" ? (
        <Notice title="Couldn't sign in" action={<Button size="sm" variant="secondary" onClick={retry}>Try again</Button>}>
          {error}
        </Notice>
      ) : (
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-card sm:p-8">
          <h1 className="font-display text-2xl font-bold">{info.title}</h1>
          <p className="mt-2 text-muted">{info.body}</p>
          {status === "ready" && wallet && (
            <p className="mt-4 flex flex-wrap items-center gap-2 text-sm">
              <span className="text-muted">Signed in as</span>
              <strong>{wallet.label}</strong>
              {roles.length === 0 ? <Badge tone="neutral">no roles</Badge> : roles.map((r) => <RoleBadge key={r} role={r} />)}
            </p>
          )}
          {status === "no-wallet" && <p className="mt-4 text-sm text-muted">You are not signed in. Use the wallet button in the top-right.</p>}
          <DemoAccounts roles={info.roles} className="mt-6" />
        </div>
      )}
    </div>
  );
}

export function RoleBadge({ role }: { role: Role }) {
  return <Badge tone={role === "Admin" ? "accent" : role === "Staff" ? "warning" : "neutral"}>{role}</Badge>;
}

let demoAccountsPromise: Promise<DemoAccount[]> | null = null;

export function useDemoAccounts() {
  const [accounts, setAccounts] = useState<DemoAccount[]>([]);
  useEffect(() => {
    demoAccountsPromise ??= api.getDemoAccounts();
    demoAccountsPromise.then(setAccounts);
  }, []);
  return accounts;
}

/** One-click sign-in as a pre-seeded demo account. Renders nothing outside local demo setups. */
export function DemoAccounts({ roles, className = "" }: { roles?: Role[]; className?: string }) {
  const accounts = useDemoAccounts().filter((a) => !roles || roles.includes(a.role));
  const { importWallet, wallet } = useAuth();
  const toast = useToast();
  if (accounts.length === 0) return null;

  return (
    <div className={className}>
      <p className="mb-2 text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">Demo accounts (local only)</p>
      <div className="flex flex-wrap gap-2">
        {accounts.map((a) => (
          <Button
            key={a.privateKey}
            size="sm"
            variant="secondary"
            onClick={() => {
              const w = importWallet(a.label, a.privateKey);
              if (w.address !== wallet?.address) toast({ tone: "success", title: `Signed in as ${a.label}` });
            }}
          >
            Use the {a.label} account
          </Button>
        ))}
      </div>
    </div>
  );
}
