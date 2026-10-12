"use client";

import { useEffect, useState } from "react";
import type { TicketSummary } from "@/lib/api";
import { formatUsdcUnits, readTicket, useChain, type OnChainTicket } from "@/lib/chain";
import { shortAddress, shortHash } from "@/lib/format";
import { CheckIcon, Spinner, XIcon } from "./ui";
import { useToast } from "./Toaster";

/** A transaction hash or address: links to the block explorer when there is one, otherwise copies. */
export function ExplorerLink({ kind, value, label }: { kind: "tx" | "address"; value?: string | null; label?: string }) {
  const { txUrl, addressUrl } = useChain();
  const toast = useToast();
  if (!value) return null;

  const text = label ?? (kind === "tx" ? shortHash(value) : shortAddress(value));
  const url = kind === "tx" ? txUrl(value) : addressUrl(value);
  const className = "font-mono text-xs text-accent hover:underline";

  if (url) {
    return (
      <a href={url} target="_blank" rel="noreferrer" className={className} title={value}>
        {text} ↗
      </a>
    );
  }
  return (
    <button
      type="button"
      className={className}
      title={`${value} (click to copy)`}
      onClick={() => {
        navigator.clipboard?.writeText(value);
        toast({ tone: "info", title: kind === "tx" ? "Transaction hash copied" : "Address copied" });
      }}
    >
      {text}
    </button>
  );
}

/** The ticket artwork exactly as the contract's tokenURI renders it. Nothing here comes from the API. */
export function TicketNftImage({ contract, tokenId, refreshKey }: { contract: `0x${string}`; tokenId: number; refreshKey?: string }) {
  const { config, client } = useChain();
  const [image, setImage] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    if (!config || !client) return;
    let cancelled = false;
    setFailed(false);
    readTicket(config, client, contract, tokenId)
      .then((t) => !cancelled && setImage(t.image))
      .catch(() => !cancelled && setFailed(true));
    return () => {
      cancelled = true;
    };
  }, [config, client, contract, tokenId, refreshKey]);

  if (failed) {
    return (
      <div className="flex aspect-[5/7] w-full items-center justify-center rounded-xl bg-surface-alt p-4 text-center text-xs text-muted">
        Couldn&apos;t read tokenURI from the chain right now.
      </div>
    );
  }
  if (!image) {
    return (
      <div className="flex aspect-[5/7] w-full items-center justify-center rounded-xl bg-surface-alt text-muted">
        <Spinner />
      </div>
    );
  }
  // eslint-disable-next-line @next/next/no-img-element -- a data: URI produced by the contract, not a remote asset
  return <img src={image} alt={`On-chain artwork of ticket #${tokenId}`} className="w-full rounded-xl" />;
}

type VerifyState =
  | { phase: "idle" }
  | { phase: "loading" }
  | { phase: "done"; data: OnChainTicket }
  | { phase: "error"; message: string };

/**
 * Reads ownership, redemption and escrow state for a ticket straight from the chain in the browser.
 * It works even if the TixFlow API is offline, which is the point: the ticket does not depend on us.
 */
export function VerifyOnChain({ ticket, walletAddress }: { ticket: TicketSummary; walletAddress?: string }) {
  const { config, client, chainName } = useChain();
  const [state, setState] = useState<VerifyState>({ phase: "idle" });

  if (!config || !client || !ticket.contractAddress || ticket.tokenId == null) return null;
  const contract = ticket.contractAddress;
  const tokenId = ticket.tokenId;

  const verify = async () => {
    setState({ phase: "loading" });
    try {
      setState({ phase: "done", data: await readTicket(config, client, contract, tokenId) });
    } catch (err) {
      setState({ phase: "error", message: err instanceof Error ? err.message.split("\n")[0] : "Chain read failed" });
    }
  };

  return (
    <div>
      <button
        type="button"
        onClick={verify}
        disabled={state.phase === "loading"}
        className="inline-flex h-8 items-center gap-1.5 rounded-lg border border-border bg-surface px-3 text-xs font-semibold transition-colors hover:border-accent hover:text-accent disabled:opacity-60"
      >
        {state.phase === "loading" ? <Spinner className="h-3 w-3" /> : <ShieldIcon />}
        Verify on-chain
      </button>

      {state.phase === "error" && <p className="mt-2 text-xs text-danger">Couldn&apos;t read the chain: {state.message}</p>}

      {state.phase === "done" && (
        <div className="animate-rise mt-3 rounded-xl border border-border bg-background p-3 text-xs">
          <p className="mb-2 font-semibold">
            Read directly from {chainName}, not from TixFlow
          </p>
          <ul className="space-y-1.5">
            <Fact
              ok={!walletAddress || state.data.owner.toLowerCase() === walletAddress.toLowerCase()}
              label="Owner"
              value={
                <>
                  <ExplorerLink kind="address" value={state.data.owner} />
                  {walletAddress && state.data.owner.toLowerCase() === walletAddress.toLowerCase() && " (this wallet)"}
                </>
              }
            />
            <Fact ok label="Status in tokenURI" value={state.data.status} />
            <Fact
              ok
              label="Paid into escrow"
              value={state.data.paid > 0n ? `${formatUsdcUnits(state.data.paid)} USDC, refundable if cancelled` : "Nothing held (free, refunded or released)"}
            />
            <Fact
              ok
              label="Transferable now"
              value={state.data.redeemed ? "No, soulbound attendance badge" : state.data.transferable ? "Yes" : "No, locked to the price-capped marketplace"}
            />
          </ul>
        </div>
      )}
    </div>
  );
}

function Fact({ ok, label, value }: { ok: boolean; label: string; value: React.ReactNode }) {
  return (
    <li className="flex items-start gap-2">
      <span className={`mt-0.5 flex h-3.5 w-3.5 shrink-0 items-center justify-center rounded-full text-white ${ok ? "bg-success" : "bg-danger"}`}>
        {ok ? <CheckIcon className="h-2.5 w-2.5" /> : <XIcon className="h-2.5 w-2.5" />}
      </span>
      <span className="min-w-0">
        <span className="text-muted">{label}: </span>
        <span className="break-words font-medium">{value}</span>
      </span>
    </li>
  );
}

function ShieldIcon() {
  return (
    <svg className="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.2} strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <path d="M12 3l8 3v6c0 4.5-3.2 8.3-8 9-4.8-.7-8-4.5-8-9V6l8-3z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}
