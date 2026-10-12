"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/lib/auth";
import { WalletMenu } from "./WalletMenu";

type Audience = "all" | "buyer" | "staff" | "admin";

const links: { href: string; label: string; show: Audience; match: (p: string) => boolean }[] = [
  { href: "/", label: "Events", show: "all", match: (p) => p === "/" || p.startsWith("/event") || p.startsWith("/checkout") },
  { href: "/tickets", label: "My tickets", show: "buyer", match: (p) => p.startsWith("/tickets") },
  { href: "/scanner", label: "Door scanner", show: "staff", match: (p) => p.startsWith("/scanner") },
  { href: "/organizer", label: "Organizer", show: "admin", match: (p) => p.startsWith("/organizer") },
  { href: "/admin", label: "Admin", show: "admin", match: (p) => p.startsWith("/admin") },
];

export function Header() {
  const pathname = usePathname() ?? "/";
  const { status, isAdmin, isStaff, isBuyer } = useAuth();

  // Navigation follows the roles of the active wallet. Before sign-in only the buyer view is offered.
  const signedIn = status === "ready";
  const visible = links.filter(
    (l) =>
      l.show === "all" ||
      (l.show === "buyer" && (!signedIn || isBuyer)) ||
      (l.show === "staff" && signedIn && isStaff) ||
      (l.show === "admin" && signedIn && isAdmin)
  );

  return (
    <header className="sticky top-[env(safe-area-inset-top,0px)] z-40 border-b border-border bg-background/85 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-6 px-4 sm:px-6">
        <Link href="/" className="flex items-center gap-2" aria-label="TixFlow home">
          <Logo />
          <span className="font-display text-xl font-bold tracking-tight">TixFlow</span>
        </Link>

        <nav className="hidden items-center gap-1 md:flex">
          {visible.map((l) => (
            <NavLink key={l.href} href={l.href} active={l.match(pathname)}>
              {l.label}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto">
          <WalletMenu />
        </div>
      </div>

      <nav className="flex gap-1 overflow-x-auto px-3 pb-2 md:hidden">
        {visible.map((l) => (
          <NavLink key={l.href} href={l.href} active={l.match(pathname)}>
            {l.label}
          </NavLink>
        ))}
      </nav>
    </header>
  );
}

function NavLink({ href, active, children }: { href: string; active: boolean; children: React.ReactNode }) {
  return (
    <Link
      href={href}
      aria-current={active ? "page" : undefined}
      className={`whitespace-nowrap rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
        active ? "bg-surface text-foreground shadow-card" : "text-muted hover:text-foreground"
      }`}
    >
      {children}
    </Link>
  );
}

function Logo() {
  return (
    <svg width="30" height="30" viewBox="0 0 32 32" aria-hidden>
      <rect width="32" height="32" rx="9" fill="var(--accent)" />
      <path
        d="M8 11.5a1.5 1.5 0 011.5-1.5h13a1.5 1.5 0 011.5 1.5v2a2.5 2.5 0 000 5v2a1.5 1.5 0 01-1.5 1.5h-13A1.5 1.5 0 018 20.5v-2a2.5 2.5 0 000-5v-2z"
        fill="#fff"
      />
      <path d="M19 10.5v11" stroke="var(--accent)" strokeWidth="1.6" strokeDasharray="1.6 1.6" />
    </svg>
  );
}
