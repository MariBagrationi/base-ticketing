"use client";

import { useCallback, useEffect, useState } from "react";
import { api, type AdminUser, type AuditEntry, type EventSummary, type Role } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { shortAddress } from "@/lib/format";
import { Avatar, Button, ButtonLink, Card, Notice, PageHeader, PageSpinner, inputClass } from "@/components/ui";
import { RoleBadge, RoleGate } from "@/components/RoleGate";
import { useToast } from "@/components/Toaster";

const ROLES: Role[] = ["User", "Staff", "Admin"];

export default function AdminPage() {
  return (
    <RoleGate need="admin">
      <AdminConsole />
    </RoleGate>
  );
}

function AdminConsole() {
  const { wallet, refreshRoles } = useAuth();
  const toast = useToast();
  const [users, setUsers] = useState<AdminUser[] | null>(null);
  const [events, setEvents] = useState<EventSummary[]>([]);
  const [audit, setAudit] = useState<AuditEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const [u, e, a] = await Promise.all([api.getUsers(), api.getEvents(), api.getAuditLog()]);
      setUsers(u);
      setEvents(e);
      setAudit(a);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Couldn't load the admin data");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  // Runs one admin action, reports the outcome, and reloads everything it may have changed.
  const run = async (action: () => Promise<{ message: string }>, affectsWallet?: string) => {
    setBusy(true);
    try {
      const result = await action();
      toast({ tone: "success", title: result.message });
      await load();
      if (affectsWallet && affectsWallet.toLowerCase() === wallet?.address.toLowerCase()) await refreshRoles();
    } catch (err) {
      toast({ tone: "error", title: "That didn't work", description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusy(false);
    }
  };

  if (!users && !error) return <PageSpinner />;

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <PageHeader
        eyebrow="Administration"
        title="Roles and access"
        description="Every change here is checked on the server, takes effect on the user's next request, and is written to the audit log."
        action={<ButtonLink href="/organizer">Manage events</ButtonLink>}
      />

      {error && (
        <div className="mb-6">
          <Notice title="Couldn't load the admin data">{error}</Notice>
        </div>
      )}

      <GrantForm disabled={busy} onGrant={(w, role) => run(() => api.grantRole(w, role), w)} />

      <h2 className="mb-3 mt-10 font-display text-lg font-bold">Accounts with roles</h2>
      <div className="space-y-3">
        {users
          ?.filter((u) => u.roles.length > 0)
          .map((u) => (
            <UserRow
              key={u.id}
              user={u}
              events={events}
              isSelf={u.wallet.toLowerCase() === wallet?.address.toLowerCase()}
              disabled={busy}
              onGrant={(role) => run(() => api.grantRole(u.wallet, role), u.wallet)}
              onRevoke={(role) => run(() => api.revokeRole(u.wallet, role), u.wallet)}
              onAssign={(eventId) => run(() => api.assignStaff(eventId, u.wallet), u.wallet)}
              onUnassign={(eventId) => run(() => api.unassignStaff(eventId, u.wallet), u.wallet)}
            />
          ))}
      </div>

      <h2 className="mb-3 mt-10 font-display text-lg font-bold">Audit log</h2>
      <Card className="overflow-hidden">
        {audit.length === 0 ? (
          <p className="p-5 text-sm text-muted">No privileged actions recorded yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[620px] text-sm">
              <thead>
                <tr className="text-left text-[11px] uppercase tracking-[0.12em] text-muted">
                  <th className="px-5 py-3 font-semibold">When</th>
                  <th className="px-3 py-3 font-semibold">Who</th>
                  <th className="px-3 py-3 font-semibold">Action</th>
                  <th className="px-3 py-3 font-semibold">Target</th>
                  <th className="px-5 py-3 font-semibold">Details</th>
                </tr>
              </thead>
              <tbody>
                {audit.map((a) => (
                  <tr key={a.id} className="border-t border-border">
                    <td className="whitespace-nowrap px-5 py-2.5 text-muted">
                      {new Date(a.createdAt).toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" })}
                    </td>
                    <td className="px-3 py-2.5 font-mono text-xs">{a.actorWallet.startsWith("0x") ? shortAddress(a.actorWallet) : a.actorWallet}</td>
                    <td className="px-3 py-2.5 font-medium">{a.action}</td>
                    <td className="px-3 py-2.5 font-mono text-xs">{a.target.startsWith("0x") ? shortAddress(a.target) : a.target.slice(0, 8)}</td>
                    <td className="px-5 py-2.5 text-muted">{a.details}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}

function GrantForm({ disabled, onGrant }: { disabled: boolean; onGrant: (wallet: string, role: Role) => void }) {
  const [wallet, setWallet] = useState("");
  const [role, setRole] = useState<Role>("Staff");
  const valid = /^0x[0-9a-fA-F]{40}$/.test(wallet.trim());

  return (
    <Card className="p-5">
      <h2 className="font-display text-lg font-bold">Grant a role</h2>
      <p className="mt-1 text-sm text-muted">
        Works for any wallet address, even before its first sign-in, so door staff can be set up ahead of an event.
      </p>
      <form
        className="mt-4 flex flex-wrap items-end gap-3"
        onSubmit={(e) => {
          e.preventDefault();
          if (valid) onGrant(wallet.trim(), role);
          setWallet("");
        }}
      >
        <div className="min-w-0 flex-1 basis-72">
          <label htmlFor="grant-wallet" className="mb-1.5 block text-sm font-medium">Wallet address</label>
          <input
            id="grant-wallet"
            value={wallet}
            onChange={(e) => setWallet(e.target.value)}
            placeholder="0x…"
            className={`${inputClass} font-mono`}
          />
        </div>
        <div>
          <label htmlFor="grant-role" className="mb-1.5 block text-sm font-medium">Role</label>
          <select id="grant-role" value={role} onChange={(e) => setRole(e.target.value as Role)} className={inputClass}>
            {ROLES.map((r) => (
              <option key={r}>{r}</option>
            ))}
          </select>
        </div>
        <Button type="submit" disabled={!valid || disabled}>Grant role</Button>
      </form>
    </Card>
  );
}

function UserRow({
  user,
  events,
  isSelf,
  disabled,
  onGrant,
  onRevoke,
  onAssign,
  onUnassign,
}: {
  user: AdminUser;
  events: EventSummary[];
  isSelf: boolean;
  disabled: boolean;
  onGrant: (role: Role) => void;
  onRevoke: (role: Role) => void;
  onAssign: (eventId: string) => void;
  onUnassign: (eventId: string) => void;
}) {
  const isStaff = user.roles.includes("Staff");
  const assigned = events.filter((e) => user.staffEventIds.includes(e.id));
  const assignable = events.filter((e) => !user.staffEventIds.includes(e.id) && !e.cancelledAt);

  return (
    <Card className="p-4 sm:p-5">
      <div className="flex flex-wrap items-center gap-3">
        <Avatar address={user.wallet} size={32} />
        <div className="min-w-0 flex-1">
          <p className="font-mono text-sm" title={user.wallet}>
            {shortAddress(user.wallet)}
            {isSelf && <span className="ml-2 font-sans text-xs text-muted">(you)</span>}
          </p>
          <p className="mt-1 flex flex-wrap items-center gap-1.5 text-xs text-muted">
            {user.roles.map((r) => (
              <RoleBadge key={r} role={r} />
            ))}
            {!user.hasSignedIn && <span>· has not signed in yet</span>}
            {user.configuredAdmin && <span>· admin set in server configuration</span>}
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {ROLES.map((role) => {
            const has = user.roles.includes(role);
            const locked = role === "Admin" && user.configuredAdmin;
            if (locked) return null;
            return (
              <Button
                key={role}
                size="sm"
                variant={has ? "secondary" : "ghost"}
                disabled={disabled}
                onClick={() => (has ? onRevoke(role) : onGrant(role))}
              >
                {has ? `Revoke ${role}` : `Grant ${role}`}
              </Button>
            );
          })}
        </div>
      </div>

      {isStaff && (
        <div className="mt-4 border-t border-border pt-4">
          <p className="text-sm font-medium">Events this checker may scan</p>
          <p className="mt-0.5 text-xs text-muted">
            {assigned.length === 0 ? "Not restricted: no assignments means every event." : "Restricted to the events below."}
          </p>
          <div className="mt-3 flex flex-wrap items-center gap-2">
            {assigned.map((e) => (
              <span key={e.id} className="inline-flex items-center gap-1.5 rounded-full bg-surface-alt py-1 pl-3 pr-1 text-xs font-medium">
                {e.name}
                <button
                  type="button"
                  aria-label={`Remove ${e.name}`}
                  disabled={disabled}
                  onClick={() => onUnassign(e.id)}
                  className="flex h-5 w-5 items-center justify-center rounded-full text-muted hover:bg-border hover:text-danger"
                >
                  ×
                </button>
              </span>
            ))}
            {assignable.length > 0 && (
              <select
                aria-label="Assign to an event"
                value=""
                disabled={disabled}
                onChange={(e) => e.target.value && onAssign(e.target.value)}
                className="h-8 rounded-lg border border-border bg-background px-2 text-xs"
              >
                <option value="">+ Assign to event…</option>
                {assignable.map((e) => (
                  <option key={e.id} value={e.id}>
                    {e.name}
                  </option>
                ))}
              </select>
            )}
          </div>
        </div>
      )}
    </Card>
  );
}
