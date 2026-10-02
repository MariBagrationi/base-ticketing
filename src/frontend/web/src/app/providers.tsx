"use client";

import { type ReactNode } from "react";
import { AuthProvider } from "@/lib/auth";
import { ToastProvider } from "@/components/Toaster";

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ToastProvider>
      <AuthProvider>{children}</AuthProvider>
    </ToastProvider>
  );
}
