"use client";

import { useEffect, useId, useRef, useState } from "react";
import type { Html5Qrcode } from "html5-qrcode";
import { Spinner } from "./ui";

// html5-qrcode states: 2 = SCANNING, 3 = PAUSED. stop() throws synchronously in any other state.
async function safeStop(scanner: Html5Qrcode | null) {
  if (!scanner) return;
  try {
    const state = scanner.getState();
    if (state === 2 || state === 3) await scanner.stop();
    scanner.clear();
  } catch {}
}

// onScan returns true when the code was accepted; later frames are then ignored.
export function CameraScanner({ onScan }: { onScan: (text: string) => boolean }) {
  const elementId = `qr-${useId().replace(/[^a-zA-Z0-9]/g, "")}`;
  const [state, setState] = useState<"starting" | "running" | "unavailable">("starting");
  const onScanRef = useRef(onScan);
  onScanRef.current = onScan;

  useEffect(() => {
    let cancelled = false;
    let fired = false;
    let scanner: Html5Qrcode | null = null;

    (async () => {
      try {
        const { Html5Qrcode } = await import("html5-qrcode");
        if (cancelled) return;
        scanner = new Html5Qrcode(elementId, { verbose: false });
        await scanner.start(
          { facingMode: "environment" },
          { fps: 10, qrbox: { width: 220, height: 220 } },
          (text) => {
            if (fired) return;
            if (onScanRef.current(text)) fired = true;
          },
          () => {}
        );
        if (cancelled) await safeStop(scanner);
        else setState("running");
      } catch {
        if (!cancelled) setState("unavailable");
      }
    })();

    return () => {
      cancelled = true;
      safeStop(scanner);
    };
  }, [elementId]);

  return (
    <div className="relative aspect-square w-full max-w-full overflow-hidden rounded-2xl bg-[#0b0e15] sm:aspect-[4/3]">
      <div id={elementId} className="h-full w-full [&_video]:h-full [&_video]:w-full [&_video]:object-cover" />
      {state !== "running" && (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 p-6 text-center text-white/70">
          {state === "starting" ? (
            <>
              <Spinner className="h-6 w-6" />
              <p className="text-sm">Starting camera…</p>
            </>
          ) : (
            <>
              <svg className="h-10 w-10 text-white/40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} aria-hidden>
                <path d="M3 7a2 2 0 012-2h2l2-2h6l2 2h2a2 2 0 012 2v10a2 2 0 01-2 2H5a2 2 0 01-2-2V7z" />
                <circle cx="12" cy="12" r="3.5" />
                <path d="M3 3l18 18" />
              </svg>
              <p className="text-sm font-medium text-white">Camera unavailable</p>
              <p className="max-w-xs text-xs">Allow camera access, or enter the ticket code below instead.</p>
            </>
          )}
        </div>
      )}
      {state === "running" && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <div className="h-[220px] w-[220px] rounded-3xl border-2 border-white/80 shadow-[0_0_0_9999px_rgb(0_0_0/0.35)]" />
        </div>
      )}
    </div>
  );
}
