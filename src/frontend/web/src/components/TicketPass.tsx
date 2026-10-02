import { QRCodeSVG } from "qrcode.react";
import type { TicketSummary } from "@/lib/api";
import { dateParts, shortHash } from "@/lib/format";
import { Badge, Spinner } from "./ui";

export function TicketStatusBadge({ status }: { status: TicketSummary["status"] }) {
  if (status === "PendingMint") return <Badge tone="warning" pulse>Minting</Badge>;
  if (status === "Redeemed") return <Badge tone="neutral">Checked in</Badge>;
  return <Badge tone="success">Valid</Badge>;
}

export function TicketPass({ ticket }: { ticket: TicketSummary }) {
  const d = dateParts(ticket.startsAt);
  const used = ticket.status === "Redeemed";
  const minting = ticket.status === "PendingMint";

  return (
    <article className="animate-rise overflow-hidden rounded-2xl border border-border bg-surface shadow-card">
      <div className="p-5">
        <div className="flex items-center justify-between gap-3">
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent">{ticket.tierName}</p>
          <TicketStatusBadge status={ticket.status} />
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
              <Spinner className="h-6 w-6" />
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
            {minting ? "Minting your NFT on Base…" : used ? "Checked in" : "Show this code at the door"}
          </p>
          <p className="mt-1 text-xs text-muted">
            {used && ticket.redeemedAt
              ? new Date(ticket.redeemedAt).toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" })
              : "The holder signs a one-time challenge to prove ownership."}
          </p>
          <p className="mt-2 truncate font-mono text-[11px] text-muted" title={ticket.id}>
            ID {ticket.id.slice(0, 8)}
            {ticket.mintTxHash && <> · tx {shortHash(ticket.mintTxHash)}</>}
          </p>
        </div>
      </div>
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
