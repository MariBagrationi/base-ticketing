"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ApiError, api, type EventDetail, type TicketSummary } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { admissionKey } from "@/lib/admission";
import { dateParts, formatCountdown, formatUsdc, jwtExpiry, shortHash } from "@/lib/format";
import {
  ArrowLeftIcon,
  Button,
  ButtonLink,
  Card,
  CheckIcon,
  Notice,
  PageSpinner,
  Spinner,
  Stepper,
} from "@/components/ui";
import { TicketPass } from "@/components/TicketPass";

const MAX_QTY = 4;

type Step =
  | { phase: "select" }
  | { phase: "reserving" }
  | { phase: "review"; orderId: string }
  | { phase: "paying"; orderId: string }
  | { phase: "minting"; orderId: string; tickets: TicketSummary[] }
  | { phase: "done"; tickets: TicketSummary[] }
  | { phase: "error"; message: string; rejoin: boolean };

export default function CheckoutPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const { status } = useAuth();
  const [event, setEvent] = useState<EventDetail | null>(null);
  const [admission, setAdmission] = useState<string | null | undefined>(undefined);
  const [tierId, setTierId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [step, setStep] = useState<Step>({ phase: "select" });
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    api
      .getEvent(eventId)
      .then((e) => {
        setEvent(e);
        const firstAvailable = e.tiers.find((t) => t.totalSupply - t.ticketsSold > 0);
        if (firstAvailable) setTierId(firstAvailable.id);
      })
      .catch(() => setEvent(null));
  }, [eventId]);

  useEffect(() => {
    if (status !== "ready") return;
    const stored = sessionStorage.getItem(admissionKey(eventId));
    if (stored && (jwtExpiry(stored) ?? Infinity) > Date.now()) {
      setAdmission(stored);
      return;
    }
    api
      .getAdmission(eventId)
      .then((a) => setAdmission(a.admissionToken))
      .catch(() => setAdmission(null));
  }, [status, eventId]);

  useEffect(() => {
    if (step.phase !== "select") return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [step.phase]);

  useEffect(() => {
    if (step.phase !== "minting") return;
    const orderId = step.orderId;
    const id = setInterval(async () => {
      try {
        const mine = (await api.getTickets()).filter((t) => t.orderId === orderId);
        if (mine.length && mine.every((t) => t.status !== "PendingMint")) setStep({ phase: "done", tickets: mine });
        else setStep((s) => (s.phase === "minting" ? { ...s, tickets: mine } : s));
      } catch {}
    }, 1500);
    return () => clearInterval(id);
  }, [step.phase, step.phase === "minting" ? step.orderId : null]);

  const reserve = useCallback(async () => {
    if (!admission) return;
    setStep({ phase: "reserving" });
    try {
      const res = await api.reserve(eventId, tierId, quantity, admission);
      sessionStorage.removeItem(admissionKey(eventId));
      setStep({ phase: "review", orderId: res.orderId });
    } catch (err) {
      const rejoin = err instanceof ApiError && (err.status === 403 || err.status === 409);
      if (rejoin) sessionStorage.removeItem(admissionKey(eventId));
      setStep({ phase: "error", message: err instanceof Error ? err.message : "Reservation failed", rejoin });
    }
  }, [admission, eventId, tierId, quantity]);

  const pay = useCallback(async (orderId: string) => {
    setStep({ phase: "paying", orderId });
    try {
      await api.confirm(orderId);
      const mine = (await api.getTickets().catch(() => [])).filter((t) => t.orderId === orderId);
      setStep({ phase: "minting", orderId, tickets: mine });
    } catch (err) {
      setStep({ phase: "error", message: err instanceof Error ? err.message : "Payment failed", rejoin: false });
    }
  }, []);

  if (event === null) {
    return (
      <Shell>
        <Notice title="Event not found" action={<Link href="/" className="font-semibold underline">Back to events</Link>} />
      </Shell>
    );
  }
  if (!event || status === "loading" || status === "signing-in" || (status === "ready" && admission === undefined)) {
    return <PageSpinner />;
  }
  if (status !== "ready") {
    return (
      <Shell>
        <Notice tone="accent" title="Connect a demo wallet to check out" action={<ButtonLink href={`/event/${eventId}`} size="sm">Go to event</ButtonLink>} />
      </Shell>
    );
  }

  const tier = event.tiers.find((t) => t.id === tierId);
  const expiresAt = admission ? jwtExpiry(admission) : null;
  const secondsLeft = expiresAt ? (expiresAt - now) / 1000 : null;
  const windowExpired = step.phase === "select" && secondsLeft !== null && secondsLeft <= 0;
  const needsQueue = step.phase === "select" && (!admission || windowExpired);
  const stepIndex = step.phase === "done" ? 3 : step.phase === "minting" ? 2 : 1;

  return (
    <Shell>
      <Link href={`/event/${eventId}`} className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted hover:text-foreground">
        <ArrowLeftIcon /> {event.name}
      </Link>

      <div className="mb-8 max-w-md">
        <Stepper steps={["Queue", "Checkout", "Minting", "Ticket"]} current={stepIndex} />
      </div>

      <div className="grid gap-8 lg:grid-cols-[1fr_340px] lg:items-start">
        <div className="min-w-0">
          {needsQueue ? (
            <Notice
              tone="warning"
              title={windowExpired ? "Your checkout window has closed" : "Join the queue first"}
              action={<ButtonLink href={`/event/${eventId}`} size="sm">Back to the queue</ButtonLink>}
            >
              {windowExpired
                ? "Admissions are held for 5 minutes so everyone in line gets a fair chance. Rejoin to get a new spot."
                : "Checkout opens once it's your turn in the queue."}
            </Notice>
          ) : step.phase === "select" || step.phase === "reserving" ? (
            <section>
              <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
                <h1 className="font-display text-2xl font-bold sm:text-3xl">Choose your tickets</h1>
                {secondsLeft !== null && (
                  <span
                    className={`rounded-full px-3 py-1 text-sm font-semibold tabular-nums ${
                      secondsLeft < 60 ? "bg-danger-soft text-danger" : "bg-accent-soft text-accent-ink"
                    }`}
                  >
                    {formatCountdown(secondsLeft)} left to check out
                  </span>
                )}
              </div>

              <fieldset>
                <legend className="mb-2 text-sm font-medium">Ticket type</legend>
                <div className="space-y-2.5">
                  {event.tiers.map((t) => {
                    const left = t.totalSupply - t.ticketsSold;
                    const selected = t.id === tierId;
                    return (
                      <label
                        key={t.id}
                        className={`flex cursor-pointer items-center gap-4 rounded-xl border-2 bg-surface p-4 transition-colors ${
                          selected ? "border-accent" : "border-border hover:border-border-strong"
                        } ${left <= 0 ? "cursor-not-allowed opacity-50" : ""}`}
                      >
                        <input
                          type="radio"
                          name="tier"
                          id={`tier-${t.id}`}
                          className="h-4 w-4 accent-[var(--accent)]"
                          checked={selected}
                          disabled={left <= 0}
                          onChange={() => {
                            setTierId(t.id);
                            setQuantity((q) => Math.min(q, Math.max(1, left)));
                          }}
                        />
                        <div className="flex-1">
                          <p className="font-semibold">{t.name}</p>
                          <p className="text-sm text-muted">{left <= 0 ? "Sold out" : `${left.toLocaleString()} left`}</p>
                        </div>
                        <p className="font-display text-lg font-bold">{formatUsdc(t.priceUsdc)}</p>
                      </label>
                    );
                  })}
                </div>
              </fieldset>

              <div className="mt-6">
                <p className="mb-2 text-sm font-medium">Quantity</p>
                <div className="inline-flex items-center rounded-xl border border-border bg-surface">
                  <button
                    aria-label="Fewer tickets"
                    className="h-11 w-11 text-lg text-muted hover:text-foreground disabled:opacity-30"
                    disabled={quantity <= 1}
                    onClick={() => setQuantity((q) => q - 1)}
                  >
                    −
                  </button>
                  <span className="w-10 text-center font-display text-lg font-bold tabular-nums">{quantity}</span>
                  <button
                    aria-label="More tickets"
                    className="h-11 w-11 text-lg text-muted hover:text-foreground disabled:opacity-30"
                    disabled={quantity >= Math.min(MAX_QTY, tier ? tier.totalSupply - tier.ticketsSold : MAX_QTY)}
                    onClick={() => setQuantity((q) => q + 1)}
                  >
                    +
                  </button>
                </div>
                <p className="mt-1.5 text-xs text-muted">Maximum {MAX_QTY} per order</p>
              </div>

              <Button size="lg" className="mt-8 w-full sm:w-auto" disabled={!tier} loading={step.phase === "reserving"} onClick={reserve}>
                Reserve {quantity} ticket{quantity === 1 ? "" : "s"}
              </Button>
            </section>
          ) : step.phase === "review" || step.phase === "paying" ? (
            <section>
              <h1 className="font-display text-2xl font-bold sm:text-3xl">Review and pay</h1>
              <p className="mt-2 text-muted">Your tickets are held. Confirm payment to mint them to your wallet.</p>
              <Card className="mt-6 p-5">
                <div className="flex items-center gap-3">
                  <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-accent-soft font-bold text-accent-ink">$</span>
                  <div className="flex-1">
                    <p className="font-semibold">USDC on Base</p>
                    <p className="text-sm text-muted">Simulated payment for this demo. No funds move.</p>
                  </div>
                </div>
              </Card>
              <Button
                size="lg"
                variant="success"
                className="mt-6 w-full sm:w-auto"
                loading={step.phase === "paying"}
                onClick={() => pay(step.orderId)}
              >
                Pay {tier ? formatUsdc(tier.priceUsdc * quantity) : ""} USDC
              </Button>
            </section>
          ) : step.phase === "minting" ? (
            <MintProgress tickets={step.tickets} />
          ) : step.phase === "done" ? (
            <section>
              <div className="mb-6 flex items-center gap-4">
                <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-success text-white">
                  <CheckIcon className="h-6 w-6" />
                </span>
                <div>
                  <h1 className="font-display text-2xl font-bold sm:text-3xl">You&apos;re going!</h1>
                  <p className="text-muted">
                    {step.tickets.length} NFT ticket{step.tickets.length === 1 ? " is" : "s are"} now in your wallet.
                  </p>
                </div>
              </div>
              <div className="grid gap-5 sm:grid-cols-2">
                {step.tickets.map((t) => (
                  <TicketPass key={t.id} ticket={t} />
                ))}
              </div>
              <div className="mt-6 flex flex-wrap gap-3">
                <ButtonLink href="/tickets">View my tickets</ButtonLink>
                <ButtonLink href="/scanner" variant="secondary">Try the door scanner</ButtonLink>
              </div>
            </section>
          ) : (
            <Notice
              title="That didn't work"
              action={
                step.rejoin ? (
                  <ButtonLink href={`/event/${eventId}`} size="sm">Rejoin the queue</ButtonLink>
                ) : (
                  <Button size="sm" variant="secondary" onClick={() => setStep({ phase: "select" })}>Try again</Button>
                )
              }
            >
              {step.message}
            </Notice>
          )}
        </div>

        <OrderSummary event={event} tierName={tier?.name} price={tier?.priceUsdc ?? 0} quantity={quantity} />
      </div>
    </Shell>
  );
}

function MintProgress({ tickets }: { tickets: TicketSummary[] }) {
  const submitted = tickets.length > 0 && tickets.every((t) => t.mintTxHash);
  const tx = tickets.find((t) => t.mintTxHash)?.mintTxHash;
  const items = [
    { label: "Payment confirmed", done: true },
    { label: submitted ? `Mint transaction sent · ${shortHash(tx)}` : "Sending mint transaction to Base", done: submitted },
    { label: "Waiting for block confirmation", done: false },
  ];
  return (
    <section>
      <h1 className="font-display text-2xl font-bold sm:text-3xl">Minting your tickets</h1>
      <p className="mt-2 text-muted">This usually takes a few seconds. You can leave; tickets appear in My tickets when ready.</p>
      <ol className="mt-6 space-y-4">
        {items.map((it, i) => {
          const active = !it.done && (i === 0 || items[i - 1].done);
          return (
            <li key={i} className="flex items-center gap-3">
              <span
                className={`flex h-7 w-7 items-center justify-center rounded-full ${
                  it.done ? "bg-success text-white" : active ? "bg-accent-soft text-accent" : "bg-surface-alt text-muted"
                }`}
              >
                {it.done ? <CheckIcon className="h-4 w-4" /> : active ? <Spinner className="h-3.5 w-3.5" /> : <span className="h-1.5 w-1.5 rounded-full bg-current" />}
              </span>
              <span className={it.done || active ? "font-medium" : "text-muted"}>{it.label}</span>
            </li>
          );
        })}
      </ol>
    </section>
  );
}

function OrderSummary({ event, tierName, price, quantity }: { event: EventDetail; tierName?: string; price: number; quantity: number }) {
  const d = dateParts(event.startsAt);
  return (
    <Card className="overflow-hidden lg:sticky lg:top-24">
      <div className="p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.14em] text-muted">Order summary</p>
        <p className="mt-2 font-display text-lg font-bold leading-tight">{event.name}</p>
        <p className="mt-1 text-sm text-muted">
          {d.weekday}, {d.month} {d.day} · {d.time}
          <br />
          {event.venueName}
        </p>
      </div>
      <div className="perforation perforation-h space-y-2 p-5 text-sm">
        <div className="flex justify-between">
          <span className="text-muted">
            {tierName ?? "Ticket"} × {quantity}
          </span>
          <span className="tabular-nums">{formatUsdc(price * quantity)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted">Network fee</span>
          <span className="text-success">Covered</span>
        </div>
        <div className="flex justify-between border-t border-border pt-2 text-base font-semibold">
          <span>Total</span>
          <span className="tabular-nums">{formatUsdc(price * quantity)} USDC</span>
        </div>
      </div>
    </Card>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  return <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">{children}</div>;
}
