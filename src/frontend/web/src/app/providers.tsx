"use client";

import { type ReactNode } from "react";
import { AuthProvider } from "@/lib/auth";
import { ChainProvider } from "@/lib/chain";
import { ToastProvider } from "@/components/Toaster";

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ToastProvider>
      <ChainProvider>
        <AuthProvider>{children}</AuthProvider>
      </ChainProvider>
    </ToastProvider>
  );
}
