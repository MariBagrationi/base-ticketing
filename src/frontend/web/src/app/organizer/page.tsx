"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { api, type EventDetail } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { dateParts, formatUsdc } from "@/lib/format";
import { Button, Card, EmptyState, Field, Notice, PageHeader, PageSpinner, TicketIcon, inputClass } from "@/components/ui";
import { useToast } from "@/components/Toaster";

export default function OrganizerPage() {
  const { status, userId, wallet, createWallet } = useAuth();
  const toast = useToast();
  const [events, setEvents] = useState<EventDetail[] | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!userId) return;
    try {
      const mine = (await api.getEvents()).filter((e) => e.organizerId === userId);
      const detailed = await Promise.all(mine.map((e) => api.getEvent(e.id)));
      setEvents(detailed.sort((a, b) => +new Date(a.startsAt) - +new Date(b.startsAt)));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Couldn't load your events");
    }
  }, [userId]);

  useEffect(() => {
    setEvents(null);
    if (status !== "ready") return;
    load();
    const id = setInterval(load, 5000);
    return () => clearInterval(id);
  }, [status, load]);

  if (status === "no-wallet") {
    return (
      <Shell>
        <PageHeader eyebrow="Organizer" title="Host an event" />
        <EmptyState
          icon={<TicketIcon />}
          title="Create an organizer wallet"
          description="Events belong to the wallet that creates them. You can keep separate wallets for organizing and buying."
          action={
            <Button
              onClick={() => {
                createWallet("Organizer");
                toast({ tone: "success", title: "Organizer wallet created" });
              }}
            >
              Create organizer wallet
            </Button>
          }
        />
      </Shell>
    );
  }

  if (status !== "ready" || (events === null && !error)) return <PageSpinner />;

  return (
    <Shell>
      <PageHeader
        eyebrow="Organizer"
        title="Your events"
        description={<>Events created by <strong className="text-foreground">{wallet?.label}</strong>. Stats refresh every few seconds.</>}
        action={!showCreate && <Button onClick={() => setShowCreate(true)}>New event</Button>}
      />

      {showCreate && (
        <CreateEventForm
          onCancel={() => setShowCreate(false)}
          onCreated={(name) => {
            setShowCreate(false);
            toast({ tone: "success", title: `“${name}” is live`, description: "It now appears on the events page." });
            load();
          }}
        />
      )}

      {error && (
        <div className="mb-6">
          <Notice title="Couldn't load your events">{error}</Notice>
        </div>
      )}

      {events && events.length === 0 && !showCreate ? (
        <EmptyState
          icon={<TicketIcon />}
          title="No events yet"
          description="Create an event with one or more ticket tiers. Buyers can join the queue right away."
          action={<Button onClick={() => setShowCreate(true)}>Create your first event</Button>}
        />
      ) : (
        <div className="space-y-6">
          {events?.map((evt) => <EventStats key={evt.id} event={evt} />)}
        </div>
      )}

      {!!events?.length && <BuyerTip />}
    </Shell>
  );
}

function BuyerTip() {
  return (
    <p className="mt-8 rounded-xl bg-accent-soft px-4 py-3 text-sm text-accent-ink">
      <strong>Tip:</strong> to buy a ticket as a customer, open the wallet menu (top-right) and switch to or create another wallet.
    </p>
  );
}

function EventStats({ event }: { event: EventDetail }) {
  const d = dateParts(event.startsAt);
  const sold = event.tiers.reduce((s, t) => s + t.ticketsSold, 0);
  const supply = event.tiers.reduce((s, t) => s + t.totalSupply, 0);
  const redeemed = event.tiers.reduce((s, t) => s + t.redeemedCount, 0);
  const revenue = event.tiers.reduce((s, t) => s + t.ticketsSold * t.priceUsdc, 0);

  return (
    <Card className="overflow-hidden">
      <div className="flex flex-wrap items-start justify-between gap-4 p-5 sm:p-6">
        <div className="min-w-0">
          <h2 className="font-display text-xl font-bold">{event.name}</h2>
          <p className="mt-1 text-sm text-muted">
            {event.venueName} · {d.weekday}, {d.month} {d.day} · {d.time}
          </p>
        </div>
        <Link href={`/event/${event.id}`} className="text-sm font-medium text-accent hover:underline">
          View public page →
        </Link>
      </div>

      <div className="grid grid-cols-1 gap-px border-y border-border bg-border sm:grid-cols-3">
        <Stat label="Tickets sold" value={sold.toLocaleString()} sub={`of ${supply.toLocaleString()} (${supply ? Math.round((sold / supply) * 100) : 0}%)`} />
        <Stat label="Checked in" value={redeemed.toLocaleString()} sub={sold ? `${Math.round((redeemed / sold) * 100)}% of sold` : "No tickets sold yet"} />
        <Stat label="Revenue" value={`${formatUsdc(revenue)}`} sub="USDC" />
      </div>

      <div className="overflow-x-auto">
        <table className="w-full min-w-[520px] text-sm">
          <thead>
            <tr className="text-left text-[11px] uppercase tracking-[0.12em] text-muted">
              <th className="px-5 py-3 font-semibold sm:px-6">Tier</th>
              <th className="px-3 py-3 font-semibold">Price</th>
              <th className="px-3 py-3 font-semibold">Sold</th>
              <th className="px-3 py-3 font-semibold">Checked in</th>
              <th className="px-5 py-3 text-right font-semibold sm:px-6">Revenue</th>
            </tr>
          </thead>
          <tbody>
            {event.tiers.map((t) => {
              const pct = t.totalSupply ? (t.ticketsSold / t.totalSupply) * 100 : 0;
              return (
                <tr key={t.id} className="border-t border-border">
                  <td className="px-5 py-3 font-medium sm:px-6">{t.name}</td>
                  <td className="px-3 py-3 tabular-nums">{formatUsdc(t.priceUsdc)}</td>
                  <td className="px-3 py-3">
                    <div className="flex items-center gap-3">
                      <span className="w-20 tabular-nums">
                        {t.ticketsSold}/{t.totalSupply}
                      </span>
                      <span className="h-1.5 w-24 overflow-hidden rounded-full bg-surface-alt">
                        <span className="block h-full rounded-full bg-accent" style={{ width: `${pct}%` }} />
                      </span>
                    </div>
                  </td>
                  <td className="px-3 py-3 tabular-nums">{t.redeemedCount}</td>
                  <td className="px-5 py-3 text-right tabular-nums sm:px-6">{formatUsdc(t.ticketsSold * t.priceUsdc)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

function Stat({ label, value, sub }: { label: string; value: string; sub: string }) {
  return (
    <div className="bg-surface px-5 py-4 sm:px-6">
      <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">{label}</p>
      <p className="mt-1 font-display text-2xl font-bold tabular-nums">{value}</p>
      <p className="text-xs text-muted">{sub}</p>
    </div>
  );
}

function defaultStart() {
  const d = new Date();
  d.setDate(d.getDate() + 14);
  d.setHours(20, 0, 0, 0);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

type TierDraft = { name: string; priceUsdc: string; totalSupply: string };

function CreateEventForm({ onCreated, onCancel }: { onCreated: (name: string) => void; onCancel: () => void }) {
  const [name, setName] = useState("");
  const [venueName, setVenueName] = useState("");
  const [description, setDescription] = useState("");
  const [startsAt, setStartsAt] = useState(defaultStart);
  const [tiers, setTiers] = useState<TierDraft[]>([
    { name: "General Admission", priceUsdc: "25", totalSupply: "200" },
    { name: "VIP", priceUsdc: "90", totalSupply: "30" },
  ]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const update = (i: number, field: keyof TierDraft, value: string) =>
    setTiers((ts) => ts.map((t, idx) => (idx === i ? { ...t, [field]: value } : t)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (new Date(startsAt) <= new Date()) return setError("Pick a date in the future.");
    const names = tiers.map((t) => t.name.trim().toLowerCase());
    if (new Set(names).size !== names.length) return setError("Each tier needs a different name.");

    setError(null);
    setSubmitting(true);
    try {
      await api.createEvent({
        name: name.trim(),
        description: description.trim(),
        venueName: venueName.trim(),
        startsAt: new Date(startsAt).toISOString(),
        tiers: tiers.map((t) => ({ name: t.name.trim(), priceUsdc: Number(t.priceUsdc), totalSupply: Number(t.totalSupply) })),
      });
      onCreated(name.trim());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Couldn't create the event");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card className="mb-8 p-5 sm:p-6">
      <form onSubmit={submit} className="space-y-5">
        <h2 className="font-display text-xl font-bold">New event</h2>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Event name" htmlFor="ev-name">
            <input id="ev-name" required value={name} onChange={(e) => setName(e.target.value)} className={inputClass} placeholder="Neon Nights Live" />
          </Field>
          <Field label="Venue" htmlFor="ev-venue">
            <input id="ev-venue" required value={venueName} onChange={(e) => setVenueName(e.target.value)} className={inputClass} placeholder="Colosseum Arena" />
          </Field>
        </div>

        <Field label="Description" htmlFor="ev-desc" hint="Shown on the event page. One or two sentences works best.">
          <textarea id="ev-desc" required rows={2} value={description} onChange={(e) => setDescription(e.target.value)} className={inputClass} placeholder="An evening of synthwave under the arches." />
        </Field>

        <Field label="Date and doors-open time" htmlFor="ev-start">
          <input id="ev-start" required type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} className={`${inputClass} sm:max-w-xs`} />
        </Field>

        <div>
          <p className="mb-2 text-sm font-medium">Ticket tiers</p>
          <div className="space-y-3">
            {tiers.map((t, i) => (
              <div key={i} className="grid grid-cols-[1fr_auto] gap-2 rounded-xl bg-surface-alt p-3 sm:grid-cols-[1fr_120px_120px_auto] sm:items-end">
                <Field label="Name" htmlFor={`tier-name-${i}`}>
                  <input id={`tier-name-${i}`} required value={t.name} onChange={(e) => update(i, "name", e.target.value)} className={inputClass} />
                </Field>
                <div className="col-span-2 grid grid-cols-2 gap-2 sm:contents">
                  <Field label="Price (USDC)" htmlFor={`tier-price-${i}`}>
                    <input id={`tier-price-${i}`} required type="number" min={0} step="0.01" value={t.priceUsdc} onChange={(e) => update(i, "priceUsdc", e.target.value)} className={inputClass} />
                  </Field>
                  <Field label="Quantity" htmlFor={`tier-supply-${i}`}>
                    <input id={`tier-supply-${i}`} required type="number" min={1} step={1} value={t.totalSupply} onChange={(e) => update(i, "totalSupply", e.target.value)} className={inputClass} />
                  </Field>
                </div>
                <button
                  type="button"
                  disabled={tiers.length === 1}
                  onClick={() => setTiers((ts) => ts.filter((_, idx) => idx !== i))}
                  className="col-start-2 row-start-1 h-10 self-end rounded-lg px-2 text-sm text-muted hover:text-danger disabled:invisible sm:col-start-auto sm:row-start-auto"
                  aria-label={`Remove tier ${t.name || i + 1}`}
                >
                  Remove
                </button>
              </div>
            ))}
          </div>
          <button
            type="button"
            onClick={() => setTiers((ts) => [...ts, { name: "", priceUsdc: "0", totalSupply: "50" }])}
            className="mt-3 text-sm font-medium text-accent hover:underline"
          >
            + Add tier
          </button>
        </div>

        {error && <Notice title="Check the form">{error}</Notice>}

        <div className="flex gap-3 border-t border-border pt-5">
          <Button type="submit" loading={submitting}>Publish event</Button>
          <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
        </div>
      </form>
    </Card>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  return <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6">{children}</div>;
}
