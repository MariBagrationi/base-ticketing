"use client";

import { useCallback, useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { privateKeyToAccount } from "viem/accounts";
import { API_BASE, api, getToken, type ScannableEvent, type TicketSummary } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { formatCountdown, shortAddress } from "@/lib/format";
import { CameraScanner } from "@/components/CameraScanner";
import { Avatar, Button, Card, CheckIcon, PageHeader, Spinner, XIcon, inputClass } from "@/components/ui";
import { RoleGate } from "@/components/RoleGate";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

type ScanState =
  | { phase: "scanning" }
  | { phase: "challenging"; ticketId: string }
  | { phase: "signing"; ticketId: string; challenge: string; expiresAt: number; eventName: string; tierName: string }
  | { phase: "verifying"; ticketId: string }
  | { phase: "admitted"; ticketId: string; eventName: string; tierName: string; checkedIn: number; sold: number }
  | { phase: "denied"; ticketId: string | null; reason: string };

/** A valid ticket held by one of the wallets in this browser, for trying the scanner without a camera. */
interface LocalTicket extends TicketSummary {
  holderLabel: string;
  holderAddress: string;
}

export default function ScannerPage() {
  return (
    <RoleGate need="staff">
      <Scanner />
    </RoleGate>
  );
}

function Scanner() {
  const { wallets, wallet, tokenFor } = useAuth();
  const [state, setState] = useState<ScanState>({ phase: "scanning" });
  const [manual, setManual] = useState("");
  const [manualError, setManualError] = useState<string | null>(null);
  const [events, setEvents] = useState<ScannableEvent[]>([]);
  const [scoped, setScoped] = useState(false);
  const [localTickets, setLocalTickets] = useState<LocalTicket[]>([]);
  const [signer, setSigner] = useState<string>("");
  const [now, setNow] = useState(() => Date.now());

  const scanning = state.phase === "scanning";

  const loadEvents = useCallback(() => {
    api
      .getScannableEvents()
      .then((r) => {
        setEvents(r.events);
        setScoped(r.scopedToAssignments);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (scanning) loadEvents();
  }, [scanning, loadEvents]);

  // Live check-in counts, pushed over SignalR to staff groups the server authorises per event.
  const eventIds = events.map((e) => e.id).join(",");
  useEffect(() => {
    if (!eventIds) return;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/queue`, { accessTokenFactory: () => getToken() ?? "" })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.None)
      .build();

    connection.on("CheckinUpdate", (update: { eventId: string; sold: number; checkedIn: number }) =>
      setEvents((list) => list.map((e) => (e.id === update.eventId ? { ...e, sold: update.sold, checkedIn: update.checkedIn } : e)))
    );
    connection
      .start()
      .then(() => Promise.all(eventIds.split(",").map((id) => connection.invoke("JoinCheckinGroup", id).catch(() => {}))))
      .catch(() => {});
    return () => {
      connection.stop().catch(() => {});
    };
  }, [eventIds]);

  // Staff see nobody's tickets through the API. For the demo, each local wallet's own session is used to
  // list the tickets that wallet holds, which is exactly what its owner would show at the door.
  useEffect(() => {
    if (!scanning) return;
    let cancelled = false;
    Promise.all(
      wallets.map(async (w) => {
        const token = tokenFor(w.address);
        if (!token) return [] as LocalTicket[];
        const tickets = await api.getTickets(token).catch(() => [] as TicketSummary[]);
        return tickets
          .filter((t) => t.status === "Minted" && t.orderStatus !== "Pending" && !t.eventCancelledAt)
          .map((t) => ({ ...t, holderLabel: w.label, holderAddress: w.address }));
      })
    ).then((lists) => !cancelled && setLocalTickets(lists.flat()));
    return () => {
      cancelled = true;
    };
  }, [scanning, wallets, tokenFor]);

  useEffect(() => {
    if (state.phase !== "signing") return;
    const id = setInterval(() => setNow(Date.now()), 500);
    return () => clearInterval(id);
  }, [state.phase]);

  const start = useCallback(
    async (raw: string, holderAddress?: string) => {
      const ticketId = raw.trim();
      if (!UUID.test(ticketId)) {
        setManualError("That doesn't look like a TixFlow ticket code.");
        return;
      }
      setManualError(null);
      setState({ phase: "challenging", ticketId });
      try {
        const res = await api.requestChallenge(ticketId);
        setSigner(holderAddress ?? wallets.find((w) => w.address !== wallet?.address)?.address ?? wallet?.address ?? "");
        setState({
          phase: "signing",
          ticketId,
          challenge: res.challenge,
          expiresAt: Date.now() + res.expiresInSeconds * 1000,
          eventName: res.eventName,
          tierName: res.tierName,
        });
      } catch (err) {
        setState({ phase: "denied", ticketId, reason: err instanceof Error ? err.message : "Couldn't verify this ticket" });
      }
    },
    [wallets, wallet]
  );

  const sign = async (ticketId: string, challenge: string) => {
    const holder = wallets.find((x) => x.address === signer);
    if (!holder) return;
    setState({ phase: "verifying", ticketId });
    try {
      const signature = await privateKeyToAccount(holder.privateKey).signMessage({ message: challenge });
      const res = await api.confirmRedeem(ticketId, challenge, signature);
      setState({ phase: "admitted", ticketId, eventName: res.eventName, tierName: res.tierName, checkedIn: res.checkedIn, sold: res.sold });
    } catch (err) {
      setState({ phase: "denied", ticketId, reason: err instanceof Error ? err.message : "Verification failed" });
    }
  };

  const reset = () => {
    setManual("");
    setState({ phase: "scanning" });
  };

  return (
    <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
      <PageHeader
        eyebrow="Venue staff"
        title="Door scanner"
        description="Scan a ticket, then the holder signs a one-time challenge with their wallet. Screenshots and copied QR codes can't pass."
      />

      {state.phase === "scanning" ? (
        <div className="space-y-6">
          <CheckinBoard events={events} scoped={scoped} />

          <div className="grid gap-6 lg:grid-cols-[1.1fr_1fr]">
            <div>
              <CameraScanner
                onScan={(text) => {
                  if (!UUID.test(text.trim())) return false;
                  start(text);
                  return true;
                }}
              />
              <form
                className="mt-4 flex gap-2"
                onSubmit={(e) => {
                  e.preventDefault();
                  start(manual);
                }}
              >
                <label htmlFor="ticket-code" className="sr-only">Ticket code</label>
                <input
                  id="ticket-code"
                  value={manual}
                  onChange={(e) => setManual(e.target.value)}
                  placeholder="Or paste a ticket code"
                  className={`${inputClass} font-mono`}
                />
                <Button type="submit" disabled={!manual.trim()}>Check</Button>
              </form>
              {manualError && <p className="mt-2 text-sm text-danger">{manualError}</p>}
            </div>

            <Card className="p-5">
              <p className="font-display text-lg font-bold">No camera? Pick a ticket</p>
              <p className="mt-1 text-sm text-muted">Valid tickets held by the buyer wallets in this browser.</p>
              {localTickets.length === 0 ? (
                <p className="mt-6 rounded-xl bg-surface-alt p-4 text-sm text-muted">
                  None of the wallets in this browser holds an unused ticket. Switch to a buyer wallet, buy one, then come
                  back as staff.
                </p>
              ) : (
                <ul className="mt-4 max-h-80 space-y-2 overflow-y-auto">
                  {localTickets.map((t) => (
                    <li key={t.id}>
                      <button
                        onClick={() => start(t.id, t.holderAddress)}
                        className="flex w-full items-center gap-3 rounded-xl border border-border p-3 text-left transition-colors hover:border-accent hover:bg-accent-soft/50"
                      >
                        <div className="min-w-0 flex-1">
                          <p className="truncate font-medium">{t.eventName}</p>
                          <p className="truncate text-xs text-muted">
                            {t.tierName} · Token #{t.tokenId} · held by {t.holderLabel}
                          </p>
                        </div>
                        <span className="text-sm font-semibold text-accent">Scan</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>
        </div>
      ) : (
        <div className="mx-auto max-w-lg">
          {state.phase === "challenging" || state.phase === "verifying" ? (
            <Card className="flex flex-col items-center gap-3 p-10 text-center">
              <Spinner className="h-7 w-7 text-accent" />
              <p className="font-medium">{state.phase === "challenging" ? "Looking up ticket…" : "Checking signature against the owner's wallet…"}</p>
            </Card>
          ) : state.phase === "signing" ? (
            <Card className="p-6">
              <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent">
                {state.tierName} · {state.eventName}
              </p>
              <h2 className="mt-1 font-display text-2xl font-bold">Holder signs to prove ownership</h2>
              <p className="mt-2 text-sm text-muted">
                At a real venue the holder signs on their own phone. In this demo, pick which wallet in this browser signs.
                Choose one that doesn&apos;t own the ticket to see a rejection.
              </p>

              <div className="mt-5 rounded-xl bg-surface-alt p-3">
                <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">One-time challenge</p>
                <p className="mt-1 break-all font-mono text-xs">{state.challenge}</p>
                <p className={`mt-2 text-xs font-medium tabular-nums ${state.expiresAt - now < 15000 ? "text-danger" : "text-muted"}`}>
                  Expires in {formatCountdown((state.expiresAt - now) / 1000)}
                </p>
              </div>

              <fieldset className="mt-5">
                <legend className="mb-2 text-sm font-medium">Signing wallet</legend>
                <div className="space-y-2">
                  {wallets.map((w) => (
                    <label
                      key={w.address}
                      className={`flex cursor-pointer items-center gap-3 rounded-xl border-2 p-3 ${signer === w.address ? "border-accent" : "border-border"}`}
                    >
                      <input
                        type="radio"
                        name="signer"
                        id={`signer-${w.address}`}
                        className="sr-only"
                        checked={signer === w.address}
                        onChange={() => setSigner(w.address)}
                      />
                      <Avatar address={w.address} size={28} />
                      <span className="flex-1 font-medium">{w.label}</span>
                      <span className="font-mono text-xs text-muted">{shortAddress(w.address)}</span>
                    </label>
                  ))}
                </div>
              </fieldset>

              <div className="mt-6 flex gap-3">
                <Button size="lg" className="flex-1" disabled={!signer || state.expiresAt <= now} onClick={() => sign(state.ticketId, state.challenge)}>
                  Sign &amp; verify
                </Button>
                <Button size="lg" variant="secondary" onClick={reset}>Cancel</Button>
              </div>
            </Card>
          ) : (
            <Result state={state} onNext={reset} />
          )}
        </div>
      )}
    </div>
  );
}

function CheckinBoard({ events, scoped }: { events: ScannableEvent[]; scoped: boolean }) {
  if (events.length === 0) return null;
  return (
    <Card className="p-5">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <p className="font-display text-lg font-bold">Check-ins</p>
        <p className="text-xs text-muted">{scoped ? "Showing the events you are assigned to" : "You can scan every event"} · updates live</p>
      </div>
      <ul className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {events.map((e) => {
          const pct = e.sold ? Math.round((e.checkedIn / e.sold) * 100) : 0;
          return (
            <li key={e.id} className="rounded-xl bg-surface-alt p-3">
              <p className="truncate text-sm font-medium" title={e.name}>{e.name}</p>
              <p className="mt-1 font-display text-xl font-bold tabular-nums">
                {e.checkedIn}
                <span className="text-sm font-normal text-muted"> / {e.sold} checked in</span>
              </p>
              <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-border">
                <div className="h-full rounded-full bg-success" style={{ width: `${pct}%` }} />
              </div>
            </li>
          );
        })}
      </ul>
    </Card>
  );
}

function Result({ state, onNext }: { state: Extract<ScanState, { phase: "admitted" | "denied" }>; onNext: () => void }) {
  const ok = state.phase === "admitted";
  return (
    <div className={`animate-rise rounded-3xl p-8 text-center text-white shadow-pop ${ok ? "bg-success" : "bg-danger"}`} role="status">
      <span className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-white/20">
        {ok ? <CheckIcon className="h-10 w-10" /> : <XIcon className="h-10 w-10" />}
      </span>
      <p className="mt-4 font-display text-4xl font-bold">{ok ? "Admitted" : "Denied"}</p>
      <p className="mt-2 text-white/90">{ok ? `${state.tierName} · ${state.eventName}` : state.reason}</p>
      {ok && (
        <p className="mt-3 text-sm text-white/80">
          {state.checkedIn} of {state.sold} checked in
        </p>
      )}
      {state.ticketId && <p className="mt-3 font-mono text-xs text-white/70">Ticket {state.ticketId.slice(0, 8)}</p>}
      <button onClick={onNext} className="mt-8 h-12 rounded-xl bg-white px-8 font-semibold text-[#0e1220] transition-opacity hover:opacity-90">
        Scan next ticket
      </button>
    </div>
  );
}
