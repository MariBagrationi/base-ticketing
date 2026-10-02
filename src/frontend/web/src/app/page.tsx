"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { api, type EventSummary } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { dateParts, formatUsdc } from "@/lib/format";
import { Badge, Button, ButtonLink, EmptyState, Notice, PageHeader, PageSpinner, TicketIcon } from "@/components/ui";
import { useToast } from "@/components/Toaster";

export default function Home() {
  const [events, setEvents] = useState<EventSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { status } = useAuth();

  useEffect(() => {
    api
      .getEvents()
      .then((list) => setEvents([...list].sort((a, b) => +new Date(a.startsAt) - +new Date(b.startsAt))))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <PageHeader
        eyebrow="Base L2 · NFT tickets"
        title="Upcoming events"
        description="Join a fair, first-come queue, check out in minutes, and get a ticket nobody can counterfeit."
        action={<ButtonLink href="/organizer" variant="secondary">Host an event</ButtonLink>}
      />

      {status === "no-wallet" && <Onboarding />}

      {loading ? (
        <PageSpinner />
      ) : error ? (
        <Notice title="Couldn't load events">{error}</Notice>
      ) : events.length === 0 ? (
        <EmptyState
          icon={<TicketIcon />}
          title="No events yet"
          description="Create the first one from the organizer dashboard. It takes under a minute."
          action={<ButtonLink href="/organizer">Create an event</ButtonLink>}
        />
      ) : (
        <div className="grid gap-5 lg:grid-cols-2">
          {events.map((evt) => (
            <EventCard key={evt.id} event={evt} />
          ))}
        </div>
      )}
    </div>
  );
}

function Onboarding() {
  const { createWallet } = useAuth();
  const toast = useToast();
  const steps = [
    ["Create a demo wallet", "Generated in your browser. No MetaMask or extension needed."],
    ["Join the fair queue", "Everyone gets a place in line. No bots skipping ahead."],
    ["Get your NFT ticket", "Pay, watch it mint, and show the QR code at the door."],
  ];

  return (
    <section className="mb-10 overflow-hidden rounded-2xl border border-accent/25 bg-surface shadow-card">
      <div className="grid gap-6 p-6 sm:p-8 lg:grid-cols-[1fr_auto] lg:items-center">
        <ol className="grid gap-5 md:grid-cols-3">
          {steps.map(([title, text], i) => (
            <li key={title} className="flex gap-3">
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-accent-soft text-sm font-bold text-accent-ink">
                {i + 1}
              </span>
              <div>
                <p className="font-semibold">{title}</p>
                <p className="mt-0.5 text-sm text-muted">{text}</p>
              </div>
            </li>
          ))}
        </ol>
        <Button
          size="lg"
          onClick={() => {
            const w = createWallet();
            toast({ tone: "success", title: `Wallet “${w.label}” is ready`, description: "Pick an event to get started." });
          }}
        >
          Create demo wallet
        </Button>
      </div>
    </section>
  );
}

function EventCard({ event }: { event: EventSummary }) {
  const d = dateParts(event.startsAt);
  const total = event.tiers.reduce((s, t) => s + t.totalSupply, 0);
  const sold = event.tiers.reduce((s, t) => s + t.ticketsSold, 0);
  const left = Math.max(0, total - sold);
  const minPrice = event.tiers.length ? Math.min(...event.tiers.map((t) => t.priceUsdc)) : 0;
  const pct = total ? Math.min(100, (sold / total) * 100) : 0;
  const soldOut = total > 0 && left === 0;

  return (
    <Link
      href={`/event/${event.id}`}
      className="group relative flex overflow-hidden rounded-2xl border border-border bg-surface shadow-card transition-all hover:-translate-y-0.5 hover:border-accent/40 hover:shadow-pop"
    >
      <div className="flex w-24 shrink-0 flex-col items-center justify-center bg-accent-soft px-2 py-5 text-accent-ink sm:w-28">
        <span className="text-xs font-bold tracking-[0.16em]">{d.month}</span>
        <span className="font-display text-4xl font-bold leading-none tabular-nums">{d.day}</span>
        <span className="mt-1 text-xs font-medium opacity-80">
          {d.weekday} · {d.time}
        </span>
      </div>

      <div className="perforation perforation-v flex min-w-0 flex-1 flex-col p-5">
        <div className="flex items-start justify-between gap-3">
          <h2 className="font-display text-xl font-bold leading-tight transition-colors group-hover:text-accent">
            {event.name}
          </h2>
          {soldOut ? (
            <Badge tone="danger">Sold out</Badge>
          ) : (
            <span className="shrink-0 text-sm text-muted">
              from <strong className="text-base text-foreground">{minPrice > 0 ? formatUsdc(minPrice) : "Free"}</strong>
            </span>
          )}
        </div>
        <p className="mt-1 text-sm text-muted">{event.venueName}</p>
        {event.description && <p className="mt-3 line-clamp-2 text-sm text-muted">{event.description}</p>}

        <div className="mt-auto pt-4">
          <div className="mb-1.5 flex justify-between text-xs">
            <span className="text-muted">
              {event.tiers.length} tier{event.tiers.length === 1 ? "" : "s"}
            </span>
            <span className="font-medium tabular-nums">
              {left.toLocaleString()} of {total.toLocaleString()} left
            </span>
          </div>
          <div className="h-1.5 overflow-hidden rounded-full bg-surface-alt">
            <div className="h-full rounded-full bg-accent" style={{ width: `${pct}%` }} />
          </div>
        </div>
      </div>
    </Link>
  );
}
