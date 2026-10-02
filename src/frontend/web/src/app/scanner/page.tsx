"use client";

import { useCallback, useEffect, useState } from "react";
import { privateKeyToAccount } from "viem/accounts";
import { api, type TicketSummary } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { formatCountdown, shortAddress } from "@/lib/format";
import { CameraScanner } from "@/components/CameraScanner";
import { Avatar, Button, Card, CheckIcon, PageHeader, Spinner, XIcon, inputClass } from "@/components/ui";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

type ScanState =
  | { phase: "scanning" }
  | { phase: "challenging"; ticketId: string }
  | { phase: "signing"; ticketId: string; challenge: string; expiresAt: number }
  | { phase: "verifying"; ticketId: string }
  | { phase: "admitted"; ticketId: string }
  | { phase: "denied"; ticketId: string | null; reason: string };

export default function ScannerPage() {
  const { wallets, wallet, status } = useAuth();
  const [state, setState] = useState<ScanState>({ phase: "scanning" });
  const [manual, setManual] = useState("");
  const [manualError, setManualError] = useState<string | null>(null);
  const [myTickets, setMyTickets] = useState<TicketSummary[]>([]);
  const [signer, setSigner] = useState<string>("");
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (status !== "ready") return;
    api
      .getTickets()
      .then((t) => setMyTickets(t.filter((x) => x.orderStatus !== "Pending")))
      .catch(() => {});
  }, [status, wallet?.address, state.phase === "scanning"]);

  useEffect(() => {
    if (wallet && !signer) setSigner(wallet.address);
  }, [wallet, signer]);

  useEffect(() => {
    if (state.phase !== "signing") return;
    const id = setInterval(() => setNow(Date.now()), 500);
    return () => clearInterval(id);
  }, [state.phase]);

  const start = useCallback(async (raw: string) => {
    const ticketId = raw.trim();
    if (!UUID.test(ticketId)) {
      setManualError("That doesn't look like a TixFlow ticket code.");
      return;
    }
    setManualError(null);
    setState({ phase: "challenging", ticketId });
    try {
      const res = await api.requestChallenge(ticketId);
      setState({ phase: "signing", ticketId, challenge: res.challenge, expiresAt: Date.now() + res.expiresInSeconds * 1000 });
    } catch (err) {
      setState({ phase: "denied", ticketId, reason: err instanceof Error ? err.message : "Couldn't verify this ticket" });
    }
  }, []);

  const sign = async (ticketId: string, challenge: string) => {
    const w = wallets.find((x) => x.address === signer);
    if (!w) return;
    setState({ phase: "verifying", ticketId });
    try {
      const signature = await privateKeyToAccount(w.privateKey).signMessage({ message: challenge });
      await api.confirmRedeem(ticketId, challenge, signature);
      setState({ phase: "admitted", ticketId });
    } catch (err) {
      setState({ phase: "denied", ticketId, reason: err instanceof Error ? err.message : "Verification failed" });
    }
  };

  const reset = () => {
    setManual("");
    setState({ phase: "scanning" });
  };

  const ticketInfo = (id: string | null) => myTickets.find((t) => t.id === id);
  const valid = myTickets.filter((t) => t.status === "Minted");

  return (
    <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
      <PageHeader
        eyebrow="Venue staff"
        title="Door scanner"
        description="Scan a ticket, then the holder signs a one-time challenge with their wallet. Screenshots and copied QR codes can't pass."
      />

      {state.phase === "scanning" ? (
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
            <p className="mt-1 text-sm text-muted">
              Valid tickets owned by {wallet ? <strong className="text-foreground">{wallet.label}</strong> : "your active wallet"}.
            </p>
            {valid.length === 0 ? (
              <p className="mt-6 rounded-xl bg-surface-alt p-4 text-sm text-muted">
                {status === "ready" ? "This wallet has no unused tickets. Buy one first, or switch wallets." : "Create a demo wallet and buy a ticket to try this."}
              </p>
            ) : (
              <ul className="mt-4 max-h-80 space-y-2 overflow-y-auto">
                {valid.map((t) => (
                  <li key={t.id}>
                    <button
                      onClick={() => start(t.id)}
                      className="flex w-full items-center gap-3 rounded-xl border border-border p-3 text-left transition-colors hover:border-accent hover:bg-accent-soft/50"
                    >
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-medium">{t.eventName}</p>
                        <p className="truncate text-xs text-muted">
                          {t.tierName} · Token #{t.tokenId} · <span className="font-mono">{t.id.slice(0, 8)}</span>
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
      ) : (
        <div className="mx-auto max-w-lg">
          {state.phase === "challenging" || state.phase === "verifying" ? (
            <Card className="flex flex-col items-center gap-3 p-10 text-center">
              <Spinner className="h-7 w-7 text-accent" />
              <p className="font-medium">{state.phase === "challenging" ? "Looking up ticket…" : "Checking signature against the owner's wallet…"}</p>
            </Card>
          ) : state.phase === "signing" ? (
            <Card className="p-6">
              <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent">Step 2 of 2</p>
              <h2 className="mt-1 font-display text-2xl font-bold">Holder signs to prove ownership</h2>
              <p className="mt-2 text-sm text-muted">
                At a real venue the holder signs on their own phone. In this demo, pick which demo wallet signs. Choose a different one to see a rejection.
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
            <Result
              ok={state.phase === "admitted"}
              ticket={ticketInfo(state.ticketId)}
              ticketId={state.ticketId}
              reason={state.phase === "denied" ? state.reason : undefined}
              onNext={reset}
            />
          )}
        </div>
      )}
    </div>
  );
}

function Result({
  ok,
  ticket,
  ticketId,
  reason,
  onNext,
}: {
  ok: boolean;
  ticket?: TicketSummary;
  ticketId: string | null;
  reason?: string;
  onNext: () => void;
}) {
  return (
    <div
      className={`animate-rise rounded-3xl p-8 text-center text-white shadow-pop ${ok ? "bg-success" : "bg-danger"}`}
      role="status"
    >
      <span className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-white/20">
        {ok ? <CheckIcon className="h-10 w-10" /> : <XIcon className="h-10 w-10" />}
      </span>
      <p className="mt-4 font-display text-4xl font-bold">{ok ? "Admitted" : "Denied"}</p>
      <p className="mt-2 text-white/90">
        {ok ? (ticket ? `${ticket.tierName} · ${ticket.eventName}` : "Ticket verified and checked in.") : reason}
      </p>
      {ticketId && <p className="mt-3 font-mono text-xs text-white/70">Ticket {ticketId.slice(0, 8)}{ticket?.tokenId != null && ` · Token #${ticket.tokenId}`}</p>}
      <button
        onClick={onNext}
        className="mt-8 h-12 rounded-xl bg-white px-8 font-semibold text-[#0e1220] transition-opacity hover:opacity-90"
      >
        Scan next ticket
      </button>
    </div>
  );
}
