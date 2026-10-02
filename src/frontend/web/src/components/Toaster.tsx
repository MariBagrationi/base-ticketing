"use client";

import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import { CheckIcon, XIcon } from "./ui";

type ToastTone = "success" | "error" | "info";
interface Toast {
  id: number;
  title: string;
  description?: string;
  tone: ToastTone;
}

const ToastContext = createContext<(toast: Omit<Toast, "id">) => void>(() => {});

export function useToast() {
  return useContext(ToastContext);
}

let nextId = 1;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const push = useCallback((toast: Omit<Toast, "id">) => {
    const id = nextId++;
    setToasts((t) => [...t.slice(-2), { ...toast, id }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 4500);
  }, []);

  return (
    <ToastContext.Provider value={push}>
      {children}
      <div
        aria-live="polite"
        className="pointer-events-none fixed inset-x-0 bottom-0 z-[100] flex flex-col items-center gap-2 p-4 pb-[calc(1rem+env(safe-area-inset-bottom,0px))] sm:items-end"
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            className="animate-rise pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl border border-border bg-surface p-3.5 shadow-pop"
          >
            <span
              className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-white ${
                t.tone === "success" ? "bg-success" : t.tone === "error" ? "bg-danger" : "bg-accent"
              }`}
            >
              {t.tone === "error" ? <XIcon className="h-3 w-3" /> : <CheckIcon className="h-3 w-3" />}
            </span>
            <div className="min-w-0 text-sm">
              <p className="font-semibold">{t.title}</p>
              {t.description && <p className="mt-0.5 text-muted">{t.description}</p>}
            </div>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
