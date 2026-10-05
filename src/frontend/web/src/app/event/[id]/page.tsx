"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import * as signalR from "@microsoft/signalr";
import { API_BASE, ApiError, api, getToken, type EventDetail } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { dateParts, formatUsdc } from "@/lib/format";
import { admissionKey } from "@/lib/admission";
import {
  ArrowLeftIcon,
  Badge,
  Button,
  Card,
  CheckIcon,
  Notice,
  PageSpinner,
  Spinner,
  Stepper,
} from "@/components/ui";
import { useToast } from "@/components/Toaster";

type QueueState =
  | { phase: "checking" }
  | { phase: "idle" }
  | { phase: "joining" }
  | { phase: "waiting"; position: number; estimate: number; lineLength: number | null }
  | { phase: "admitted"; token: string }
  | { phase: "error"; message: string };

export default function EventPage() {
  const { id: eventId } = useParams<{ id: string }>();
  const [event, setEvent] = useState<EventDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const { userId } = useAuth();

  useEffect(() => {
    api
      .getEvent(eventId)
      .then(setEvent)
      .catch((err) => setLoadError(err instanceof ApiError && err.status === 404 ? "This event doesn't exist." : err.message))
      .finally(() => setLoading(false));
  }, [eventId]);

  if (loading) return <PageSpinner />;

  if (!event) {
    return (
      <div className="mx-auto max-w-xl px-4 py-20">
        <Notice title="Event unavailable" action={<Link href="/" className="font-semibold underline">Back to events</Link>}>
          {loadError}
        </Notice>
      </div>
    );
  }

  const d = dateParts(event.startsAt);

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <Link href="/" className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted hover:text-foreground">
        <ArrowLeftIcon /> All events
      </Link>

      <div className="grid gap-8 lg:grid-cols-[1fr_380px] lg:items-start">
        <div>
          <div className="flex items-start gap-5">
            <div className="flex w-20 shrink-0 flex-col items-center rounded-2xl bg-accent-soft py-3 text-accent-ink">
              <span className="text-xs font-bold tracking-[0.16em]">{d.month}</span>
              <span className="font-display text-4xl font-bold leading-none">{d.day}</span>
            </div>
            <div className="min-w-0">
              {event.organizerId === userId && <Badge tone="accent">You&apos;re the organizer</Badge>}
              <h1 className="mt-1 font-display text-3xl font-bold tracking-tight sm:text-4xl">{event.name}</h1>
              <p className="mt-2 text-muted">
                {event.venueName} · {d.long} · {d.time}
              </p>
            </div>
          </div>

          {event.description && (
            <p className="mt-6 max-w-[65ch] whitespace-pre-line leading-relaxed text-foreground/90">{event.description}</p>
          )}

          <h2 className="mb-3 mt-10 font-display text-lg font-bold">Tickets</h2>
          <div className="space-y-3">
            {event.tiers.map((tier) => {
              const left = tier.totalSupply - tier.ticketsSold;
              return (
                <div
                  key={tier.id}
                  className={`flex items-center justify-between gap-4 rounded-xl border border-border bg-surface p-4 ${left <= 0 ? "opacity-60" : ""}`}
                >
                  <div>
                    <p className="font-semibold">{tier.name}</p>
                    <p className="text-sm text-muted">
                      {left <= 0 ? "Sold out" : `${left.toLocaleString()} of ${tier.totalSupply.toLocaleString()} left`}
                    </p>
                  </div>
                  <p className="font-display text-xl font-bold">{formatUsdc(tier.priceUsdc)}</p>
                </div>
              );
            })}
          </div>
        </div>

        <aside className="lg:sticky lg:top-24">
          <PurchasePanel eventId={eventId} soldOut={event.tiers.every((t) => t.totalSupply - t.ticketsSold <= 0)} />
        </aside>
      </div>
    </div>
  );
}

function PurchasePanel({ eventId, soldOut }: { eventId: string; soldOut: boolean }) {
  const router = useRouter();
  const toast = useToast();
  const { status, error, createWallet, retry } = useAuth();
  const [queue, setQueue] = useState<QueueState>({ phase: "checking" });
  const stopRef = useRef<() => void>(() => {});

  useEffect(() => () => stopRef.current(), []);

  const watch = useCallback(
    async (join: boolean) => {
      stopRef.current();
      if (join) setQueue({ phase: "joining" });
      let stopped = false;
      let poll: ReturnType<typeof setInterval> | undefined;

      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE}/hubs/queue`, { accessTokenFactory: () => getToken() ?? "" })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.None)
        .build();

      const stop = () => {
        stopped = true;
        if (poll) clearInterval(poll);
        connection.stop().catch(() => {});
      };
      stopRef.current = stop;

      const admit = (token: string) => {
        if (stopped) return;
        stop();
        sessionStorage.setItem(admissionKey(eventId), token);
        setQueue({ phase: "admitted", token });
      };

      connection.on("Admitted", (data: { admissionToken: string }) => admit(data.admissionToken));
      connection.on("QueueUpdate", (data: { queueLength: number }) =>
        setQueue((prev) => (prev.phase === "waiting" ? { ...prev, lineLength: data.queueLength } : prev))
      );

      // Subscribe before joining so an immediate admission can't be missed; polling covers a failed socket.
      try {
        await connection.start();
        await connection.invoke("JoinQueueGroup", eventId);
      } catch {}
      if (stopped) return;

      if (join) {
        try {
          const res = await api.joinQueue(eventId);
          if (stopped) return;
          setQueue({ phase: "waiting", position: res.position, estimate: res.estimatedWaitSeconds, lineLength: null });
        } catch (err) {
          stop();
          setQueue({ phase: "error", message: err instanceof Error ? err.message : "Couldn't join the queue" });
          return;
        }
      }

      const tick = async () => {
        try {
          const pos = await api.getPosition(eventId);
          setQueue((prev) =>
            prev.phase === "waiting" ? { ...prev, position: pos.position, estimate: pos.estimatedWaitSeconds } : prev
          );
        } catch (err) {
          if (err instanceof ApiError && err.status === 404) {
            api.getAdmission(eventId).then((a) => admit(a.admissionToken)).catch(() => {});
          }
        }
      };
      poll = setInterval(tick, 2000);
    },
    [eventId]
  );

  useEffect(() => {
    stopRef.current();
    setQueue({ phase: "checking" });
    if (status !== "ready") return;
    let cancelled = false;
    (async () => {
      try {
        const pos = await api.getPosition(eventId);
        if (cancelled) return;
        setQueue({ phase: "waiting", position: pos.position, estimate: pos.estimatedWaitSeconds, lineLength: null });
        watch(false);
      } catch {
        try {
          const a = await api.getAdmission(eventId);
          if (cancelled) return;
          sessionStorage.setItem(admissionKey(eventId), a.admissionToken);
          setQueue({ phase: "admitted", token: a.admissionToken });
        } catch {
          if (!cancelled) setQueue({ phase: "idle" });
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [status, eventId, watch]);

  useEffect(() => {
    if (queue.phase !== "admitted") return;
    const t = setTimeout(() => router.push(`/checkout/${eventId}`), 1500);
    return () => clearTimeout(t);
  }, [queue.phase, eventId, router]);

  const leave = async () => {
    stopRef.current();
    setQueue({ phase: "idle" });
    await api.leaveQueue(eventId).catch(() => {});
    toast({ tone: "info", title: "You left the queue" });
  };

  const step = queue.phase === "admitted" ? 1 : 0;

  return (
    <Card className="p-6">
      <Stepper steps={["Queue", "Checkout", "Ticket"]} current={step} />
      <div className="mt-6">
        {status === "loading" || status === "signing-in" ? (
          <Centered>
            <Spinner className="h-6 w-6 text-accent" />
            <p className="text-sm text-muted">Signing in with your demo wallet…</p>
          </Centered>
        ) : status === "no-wallet" ? (
          <div className="text-center">
            <p className="font-display text-lg font-bold">Ready to get tickets?</p>
            <p className="mt-1 text-sm text-muted">Create a demo wallet to join the queue. It takes one click.</p>
            <Button
              size="lg"
              className="mt-5 w-full"
              onClick={() => {
                const w = createWallet();
                toast({ tone: "success", title: `Wallet “${w.label}” created` });
              }}
            >
              Create demo wallet
            </Button>
          </div>
        ) : status === "error" ? (
          <Notice title="Couldn't sign in" action={<Button size="sm" variant="secondary" onClick={retry}>Try again</Button>}>
            {error}
          </Notice>
        ) : queue.phase === "checking" ? (
          <Centered>
            <Spinner className="h-6 w-6 text-accent" />
          </Centered>
        ) : queue.phase === "idle" || queue.phase === "error" ? (
          <div>
            {queue.phase === "error" && (
              <div className="mb-4">
                <Notice title="Couldn't join the queue">{queue.message}</Notice>
              </div>
            )}
            <Button size="lg" className="w-full" disabled={soldOut} onClick={() => watch(true)}>
              {soldOut ? "Sold out" : "Join the queue"}
            </Button>
            <ul className="mt-4 space-y-2 text-sm text-muted">
              <li className="flex gap-2"><Dot /> First come, first served. Bots can&apos;t skip ahead.</li>
              <li className="flex gap-2"><Dot /> When it&apos;s your turn you get a 5-minute checkout window.</li>
              <li className="flex gap-2"><Dot /> Up to 4 tickets per order.</li>
            </ul>
          </div>
        ) : queue.phase === "joining" ? (
          <Centered>
            <Spinner className="h-6 w-6 text-accent" />
            <p className="text-sm text-muted">Getting you a place in line…</p>
          </Centered>
        ) : queue.phase === "waiting" ? (
          <WaitingView position={queue.position} estimate={queue.estimate} lineLength={queue.lineLength} onLeave={leave} />
        ) : (
          <Centered>
            <span className="flex h-14 w-14 items-center justify-center rounded-full bg-success text-white">
              <CheckIcon className="h-7 w-7" />
            </span>
            <p className="font-display text-xl font-bold">It&apos;s your turn!</p>
            <p className="text-sm text-muted">Taking you to checkout…</p>
            <Button className="mt-2" onClick={() => router.push(`/checkout/${eventId}`)}>
              Go to checkout now
            </Button>
          </Centered>
        )}
      </div>
    </Card>
  );
}

function WaitingView({
  position,
  estimate,
  lineLength,
  onLeave,
}: {
  position: number;
  estimate: number;
  lineLength: number | null;
  onLeave: () => void;
}) {
  return (
    <div className="text-center">
      <p className="text-sm font-medium text-muted">You&apos;re in line</p>
      <div className="relative mx-auto my-4 flex h-32 w-32 items-center justify-center">
        <svg className="absolute inset-0 animate-spin [animation-duration:2.4s]" viewBox="0 0 100 100" aria-hidden>
          <circle cx="50" cy="50" r="45" fill="none" stroke="var(--surface-alt)" strokeWidth="6" />
          <circle cx="50" cy="50" r="45" fill="none" stroke="var(--accent)" strokeWidth="6" strokeLinecap="round" strokeDasharray="70 213" />
        </svg>
        <div>
          <p className="font-display text-4xl font-bold tabular-nums">{position}</p>
          <p className="text-[11px] font-semibold uppercase tracking-wider text-muted">ahead of you</p>
        </div>
      </div>
      <p className="text-sm">
        {position === 0 ? "You're next. Hang tight…" : `About ${estimate < 60 ? `${Math.max(estimate, 2)} seconds` : `${Math.ceil(estimate / 60)} min`} to go`}
      </p>
      {lineLength != null && <p className="mt-1 text-xs text-muted">{lineLength.toLocaleString()} people still waiting</p>}
      <p className="mt-4 text-xs text-muted">Keep this page open. We&apos;ll move you to checkout automatically.</p>
      <Button variant="ghost" size="sm" className="mt-3" onClick={onLeave}>
        Leave queue
      </Button>
    </div>
  );
}

function Centered({ children }: { children: React.ReactNode }) {
  return <div className="flex flex-col items-center gap-3 py-6 text-center">{children}</div>;
}

function Dot() {
  return <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-accent" />;
}
