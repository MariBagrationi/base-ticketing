"use client";

import { useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import type { TicketSummary } from "@/lib/api";
import { useChain } from "@/lib/chain";
import { dateParts, shortHash } from "@/lib/format";
import { Badge, Spinner } from "./ui";
import { ExplorerLink, TicketNftImage, VerifyOnChain } from "./OnChain";

export function TicketStatusBadge({ ticket }: { ticket: TicketSummary }) {
  if (ticket.refundedAt) return <Badge tone="neutral">Refunded</Badge>;
  if (ticket.eventCancelledAt) return <Badge tone="danger">Event cancelled</Badge>;
  if (ticket.status === "PendingMint") {
    return ticket.mintStatus === "Failed" ? <Badge tone="danger">Mint failed</Badge> : <Badge tone="warning" pulse>Minting</Badge>;
  }
  if (ticket.status === "Redeemed") {
    return ticket.badgeClaimedAt ? <Badge tone="accent">Attendance badge</Badge> : <Badge tone="neutral">Checked in</Badge>;
  }
  return <Badge tone="success">Valid</Badge>;
}

export function TicketPass({ ticket, walletAddress }: { ticket: TicketSummary; walletAddress?: string }) {
  const { config } = useChain();
  const [showNft, setShowNft] = useState(false);
  const d = dateParts(ticket.startsAt);
  const used = ticket.status === "Redeemed";
  const minting = ticket.status === "PendingMint";
  const failed = minting && ticket.mintStatus === "Failed";
  const onChain = !!config && !!ticket.contractAddress && ticket.tokenId != null;

  return (
    <article className="animate-rise overflow-hidden rounded-2xl border border-border bg-surface shadow-card">
      <div className="p-5">
        <div className="flex items-center justify-between gap-3">
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent">{ticket.tierName}</p>
          <TicketStatusBadge ticket={ticket} />
        </div>
        <h3 className="mt-2 font-display text-xl font-bold leading-tight">{ticket.eventName}</h3>

        <dl className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
          <Detail label="Date" value={`${d.weekday}, ${d.month} ${d.day}`} />
          <Detail label="Doors" value={d.time} />
          <Detail label="Venue" value={ticket.venueName} />
          <Detail label="Token" value={ticket.tokenId != null ? `#${ticket.tokenId}` : "Pending"} mono />
        </dl>
      </div>

      <div className="perforation perforation-h flex items-center gap-4 bg-surface-alt/60 p-5">
        <div className="relative shrink-0 rounded-xl bg-white p-2.5">
          {minting ? (
            <div className="flex h-[104px] w-[104px] items-center justify-center text-[#5b6275]">
              {failed ? <span className="text-2xl font-bold">!</span> : <Spinner className="h-6 w-6" />}
            </div>
          ) : (
            <QRCodeSVG value={ticket.id} size={104} level="M" className={used ? "opacity-25" : ""} />
          )}
          {used && (
            <span className="absolute inset-0 flex items-center justify-center">
              <span className="-rotate-12 rounded-md border-2 border-[#5b6275] px-2 py-0.5 text-xs font-bold uppercase tracking-wider text-[#5b6275]">
                Used
              </span>
            </span>
          )}
        </div>
        <div className="min-w-0 text-sm">
          <p className="font-medium">
            {failed
              ? "Minting failed"
              : minting
                ? "Minting your NFT on Base…"
                : used
                  ? ticket.badgeClaimedAt
                    ? "Soulbound proof of attendance"
                    : "Checked in"
                  : "Show this code at the door"}
          </p>
          <p className="mt-1 text-xs text-muted">
            {failed
              ? (ticket.mintError ?? "The mint transaction was rejected.")
              : used && ticket.redeemedAt
                ? new Date(ticket.redeemedAt).toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" })
                : "The holder signs a one-time challenge to prove ownership."}
          </p>
          <p className="mt-2 truncate font-mono text-[11px] text-muted" title={ticket.id}>
            ID {ticket.id.slice(0, 8)}
            {!config && ticket.mintTxHash && <> · tx {shortHash(ticket.mintTxHash)}</>}
          </p>
        </div>
      </div>

      {onChain && (
        <div className="border-t border-border p-5">
          <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
            <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">On-chain</p>
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
              <span className="text-xs text-muted">
                contract <ExplorerLink kind="address" value={ticket.contractAddress} />
              </span>
              {ticket.mintTxHash && (
                <span className="text-xs text-muted">
                  mint tx <ExplorerLink kind="tx" value={ticket.mintTxHash} />
                </span>
              )}
            </div>
          </div>

          <div className="mt-3 flex flex-wrap items-start gap-3">
            <VerifyOnChain ticket={ticket} walletAddress={walletAddress} />
            <button
              type="button"
              onClick={() => setShowNft((s) => !s)}
              aria-expanded={showNft}
              className="inline-flex h-8 items-center rounded-lg px-3 text-xs font-semibold text-muted transition-colors hover:bg-surface-alt hover:text-foreground"
            >
              {showNft ? "Hide NFT" : "Show NFT"}
            </button>
          </div>

          {showNft && (
            <div className="animate-rise mx-auto mt-4 max-w-[220px]">
              <TicketNftImage
                contract={ticket.contractAddress!}
                tokenId={ticket.tokenId!}
                refreshKey={ticket.badgeClaimedAt ?? ticket.status}
              />
              <p className="mt-2 text-center text-[11px] text-muted">Rendered by the contract&apos;s tokenURI. No server involved.</p>
            </div>
          )}
        </div>
      )}
    </article>
  );
}

function Detail({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="min-w-0">
      <dt className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted">{label}</dt>
      <dd className={`mt-0.5 break-words font-medium ${mono ? "font-mono" : ""}`}>{value}</dd>
    </div>
  );
}
