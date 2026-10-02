export function shortAddress(address?: string | null) {
  if (!address) return "";
  return `${address.slice(0, 6)}…${address.slice(-4)}`;
}

export function shortHash(hash?: string | null) {
  if (!hash) return "";
  return `${hash.slice(0, 10)}…${hash.slice(-6)}`;
}

export function formatUsdc(amount: number) {
  return `$${amount.toLocaleString("en-US", {
    minimumFractionDigits: Number.isInteger(amount) ? 0 : 2,
    maximumFractionDigits: 2,
  })}`;
}

export function dateParts(iso: string) {
  const d = new Date(iso);
  return {
    month: d.toLocaleDateString("en-US", { month: "short" }).toUpperCase(),
    day: d.getDate(),
    weekday: d.toLocaleDateString("en-US", { weekday: "short" }),
    time: d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" }),
    long: d.toLocaleDateString("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
      year: "numeric",
    }),
  };
}

export function formatCountdown(seconds: number) {
  const s = Math.max(0, Math.floor(seconds));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;
}

export function avatarGradient(address: string) {
  const hex = address.toLowerCase().replace(/^0x/, "");
  const h1 = parseInt(hex.slice(0, 6), 16) % 360;
  const h2 = (h1 + 40 + (parseInt(hex.slice(6, 10), 16) % 120)) % 360;
  return `linear-gradient(135deg, hsl(${h1} 85% 58%), hsl(${h2} 80% 45%))`;
}

// Reads the `exp` claim so the checkout timer matches the server-side admission window.
export function jwtExpiry(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
    return typeof payload.exp === "number" ? payload.exp * 1000 : null;
  } catch {
    return null;
  }
}
