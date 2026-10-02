"use client";

import { useEffect, useState } from "react";
import { api, type TicketSummary } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { ButtonLink, EmptyState, Notice, PageHeader, PageSpinner, TicketIcon } from "@/components/ui";
import { TicketPass } from "@/components/TicketPass";

export default function TicketsPage() {
  const { status, wallet } = useAuth();
  const [tickets, setTickets] = useState<TicketSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (status !== "ready") return;
    let cancelled = false;
    let timer: ReturnType<typeof setTimeout>;
    const load = async () => {
      try {
        const all = (await api.getTickets()).filter((t) => t.orderStatus !== "Pending");
        if (cancelled) return;
        setTickets(all);
        setError(null);
        if (all.some((t) => t.status === "PendingMint")) timer = setTimeout(load, 2000);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Couldn't load tickets");
      }
    };
    setTickets(null);
    load();
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [status, wallet?.address]);

  const upcoming = tickets?.filter((t) => t.status !== "Redeemed") ?? [];
  const used = tickets?.filter((t) => t.status === "Redeemed") ?? [];

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <PageHeader
        title="My tickets"
        description={wallet ? <>Tickets owned by <strong className="text-foreground">{wallet.label}</strong>. Switch wallets from the menu in the top-right.</> : undefined}
      />

      {status === "no-wallet" ? (
        <EmptyState
          icon={<TicketIcon />}
          title="No wallet yet"
          description="Create a demo wallet with the button in the top-right, then buy a ticket."
        />
      ) : error ? (
        <Notice title="Couldn't load your tickets">{error}</Notice>
      ) : tickets === null ? (
        <PageSpinner />
      ) : tickets.length === 0 ? (
        <EmptyState
          icon={<TicketIcon />}
          title="No tickets yet"
          description="When you buy a ticket it shows up here with a QR code for the door."
          action={<ButtonLink href="/">Browse events</ButtonLink>}
        />
      ) : (
        <div className="space-y-10">
          {upcoming.length > 0 && (
            <section>
              <h2 className="mb-4 font-display text-lg font-bold">Upcoming · {upcoming.length}</h2>
              <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                {upcoming.map((t) => (
                  <TicketPass key={t.id} ticket={t} />
                ))}
              </div>
            </section>
          )}
          {used.length > 0 && (
            <section>
              <h2 className="mb-4 font-display text-lg font-bold text-muted">Checked in · {used.length}</h2>
              <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                {used.map((t) => (
                  <TicketPass key={t.id} ticket={t} />
                ))}
              </div>
            </section>
          )}
        </div>
      )}
    </div>
  );
}
