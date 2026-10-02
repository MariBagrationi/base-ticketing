"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";

type Health = Awaited<ReturnType<typeof api.getChainHealth>>;

export function ChainHealthBanner() {
  const [health, setHealth] = useState<Health | null>(null);
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
    const id = setInterval(check, 20000);
    return () => clearInterval(id);
  }, []);

  if (offline) {
    return (
      <Strip className="bg-danger-soft text-danger" dot="bg-danger">
        Can&apos;t reach the TixFlow API. Start the backend and this page will reconnect automatically.
      </Strip>
    );
  }
  if (!health || health.status === "Healthy") return null;

  if (health.status === "Simulated") {
    return (
      <Strip className="bg-accent-soft text-accent-ink" dot="bg-accent">
        <strong className="font-semibold">Demo mode</strong> · Payments and NFT minting are simulated locally.
      </Strip>
    );
  }

  return (
    <Strip className="bg-warning-soft text-warning" dot="bg-warning animate-pulse">
      {health.message}
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
