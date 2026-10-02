export const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5222";

let authToken: string | null = null;
let reauthenticate: (() => Promise<string | null>) | null = null;

export function getToken() {
  return authToken;
}

export function setToken(token: string | null) {
  authToken = token;
}

export function setReauthenticate(fn: (() => Promise<string | null>) | null) {
  reauthenticate = fn;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message);
  }
}

async function request<T>(path: string, options: RequestInit = {}, retried = false): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };
  if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

  let res: Response;
  try {
    res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  } catch {
    throw new ApiError(0, "Can't reach the TixFlow server. Check that the API is running on " + API_BASE + ".");
  }

  if (res.status === 401 && authToken && reauthenticate && !retried) {
    const fresh = await reauthenticate();
    if (fresh) return request<T>(path, options, true);
  }

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new ApiError(res.status, body.message || body.error || fallbackMessage(res.status));
  }

  return res.json();
}

function fallbackMessage(status: number) {
  switch (status) {
    case 401:
      return "Your session expired. Reconnect your wallet.";
    case 403:
      return "You don't have access to do that.";
    case 404:
      return "Not found.";
    case 429:
      return "Too many requests. Wait a few seconds and try again.";
    default:
      return `Something went wrong on the server (HTTP ${status}).`;
  }
}

export const api = {
  getNonce: () => request<{ nonce: string }>("/auth/nonce"),

  verify: (message: string, signature: string) =>
    request<{ token: string }>("/auth/verify", {
      method: "POST",
      body: JSON.stringify({ message, signature }),
    }),

  getMe: () => request<{ userId: string; wallet: string }>("/auth/me"),

  getEvents: () => request<EventSummary[]>("/events"),

  getEvent: (id: string) => request<EventDetail>(`/events/${id}`),

  createEvent: (data: CreateEventPayload) =>
    request<{ id: string; name: string }>("/events", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  joinQueue: (eventId: string) =>
    request<{ position: number; estimatedWaitSeconds: number }>(`/queue/${eventId}/join`, {
      method: "POST",
    }),

  leaveQueue: (eventId: string) =>
    request<{ removed: boolean }>(`/queue/${eventId}/leave`, { method: "POST" }),

  getPosition: (eventId: string) =>
    request<{ position: number; estimatedWaitSeconds: number }>(`/queue/${eventId}/position`),

  getAdmission: (eventId: string) =>
    request<{ admissionToken: string }>(`/queue/${eventId}/admission`),

  reserve: (eventId: string, tierId: string, quantity: number, admissionToken: string) =>
    request<{ orderId: string; quantity: number }>("/checkout/reserve", {
      method: "POST",
      body: JSON.stringify({ eventId, tierId, quantity }),
      headers: { "X-Admission-Token": admissionToken },
    }),

  confirm: (orderId: string) =>
    request<{ orderId: string; mintJobsCreated: number }>("/checkout/confirm", {
      method: "POST",
      body: JSON.stringify({ orderId, txHash: "0x" + "0".repeat(64) }),
    }),

  getTickets: () => request<TicketSummary[]>("/tickets"),

  getChainHealth: () =>
    request<{ status: "Healthy" | "Simulated" | "Degraded"; message: string }>("/health/chain"),

  requestChallenge: (ticketId: string) =>
    request<{ ticketId: string; challenge: string; expiresInSeconds: number }>(
      `/redeem/${ticketId}/challenge`,
      { method: "POST" }
    ),

  confirmRedeem: (ticketId: string, challenge: string, signature: string) =>
    request<{ success: boolean; ticketId: string; redeemedAt: string }>(`/redeem/${ticketId}/confirm`, {
      method: "POST",
      body: JSON.stringify({ challenge, signature }),
    }),
};

export interface TierSummary {
  id: string;
  name: string;
  priceUsdc: number;
  totalSupply: number;
  ticketsSold: number;
}

export interface TierDetail extends TierSummary {
  redeemedCount: number;
}

export interface EventSummary {
  id: string;
  name: string;
  description: string;
  venueName: string;
  startsAt: string;
  organizerId: string;
  tiers: TierSummary[];
}

export interface EventDetail extends Omit<EventSummary, "tiers"> {
  tiers: TierDetail[];
}

export type TicketStatus = "PendingMint" | "Minted" | "Redeemed";

export interface TicketSummary {
  id: string;
  status: TicketStatus;
  tokenId: number | null;
  orderId: string;
  ticketTierId: string;
  tierName: string;
  priceUsdc: number;
  eventName: string;
  eventId: string;
  venueName: string;
  startsAt: string;
  orderStatus: "Pending" | "Confirmed" | string;
  mintTxHash: string | null;
  redeemedAt: string | null;
  createdAt: string;
}

export interface CreateEventPayload {
  name: string;
  description: string;
  venueName: string;
  startsAt: string;
  tiers: { name: string; priceUsdc: number; totalSupply: number }[];
}
