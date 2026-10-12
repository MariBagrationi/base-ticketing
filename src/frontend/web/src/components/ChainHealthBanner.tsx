"use client";

import { useEffect, useState } from "react";
import { api, type ChainHealth } from "@/lib/api";
import { useChain } from "@/lib/chain";

export function ChainHealthBanner() {
  const { chainName } = useChain();
  const [health, setHealth] = useState<ChainHealth | null>(null);
  const [offline, setOffline] = useState(false);

  useEffect(() => {
    const check = () =>
      api
        .getChainHealth()
        .then((h) => {
          setHealth(h);
          setOffline(false);
        })
        .catch(() => setOffline(true));
    check();
    const id = setInterval(check, 5000);
    return () => clearInterval(id);
  }, []);

  if (offline) {
    return (
      <Strip className="bg-danger-soft text-danger" dot="bg-danger">
        Can&apos;t reach the TixFlow API. Start the backend and this page will reconnect automatically.
      </Strip>
    );
  }
  if (!health) return null;

  if (health.status === "Simulated") {
    return (
      <Strip className="bg-accent-soft text-accent-ink" dot="bg-accent">
        <strong className="font-semibold">Demo mode</strong> · Payments and NFT minting are simulated locally.
      </Strip>
    );
  }

  if (health.status === "Healthy") {
    return (
      <Strip className="bg-success-soft text-success" dot="bg-success">
        <strong className="font-semibold">{chainName || "Chain"} connected</strong>
        {health.headBlock != null && <> · block {health.headBlock.toLocaleString()}</>}
        {!!health.pendingMints && (
          <>
            {" "}
            · {health.pendingMints} mint{health.pendingMints === 1 ? "" : "s"} in progress
          </>
        )}
      </Strip>
    );
  }

  return (
    <Strip className="bg-warning-soft text-warning" dot="bg-warning animate-pulse">
      <strong className="font-semibold">Chain unreachable.</strong> Checkout still works: purchases are saved and minted
      automatically when it returns.
      {!!health.pendingMints && <> {health.pendingMints} waiting.</>}
    </Strip>
  );
}

function Strip({ className, dot, children }: { className: string; dot: string; children: React.ReactNode }) {
  return (
    <div role="status" className={`px-4 py-2 text-center text-xs sm:text-[13px] ${className}`}>
      <span className={`mr-2 inline-block h-1.5 w-1.5 -translate-y-px rounded-full align-middle ${dot}`} />
      {children}
    </div>
  );
}
