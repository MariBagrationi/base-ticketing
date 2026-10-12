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

interface RequestOptions extends RequestInit {
  /** Send this token instead of the active wallet's. */
  token?: string;
}

async function request<T>(path: string, options: RequestOptions = {}, retried = false): Promise<T> {
  const { token, ...init } = options;
  const bearer = token ?? authToken;
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(init.headers as Record<string, string>),
  };
  if (bearer) headers["Authorization"] = `Bearer ${bearer}`;

  let res: Response;
  try {
    res = await fetch(`${API_BASE}${path}`, { ...init, headers });
  } catch {
    throw new ApiError(0, "Can't reach the TixFlow server. Check that the API is running on " + API_BASE + ".");
  }

  if (res.status === 401 && !token && authToken && reauthenticate && !retried) {
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

let chainConfigPromise: Promise<ChainConfig> | null = null;

export const api = {
  getNonce: () => request<{ nonce: string }>("/auth/nonce"),

  verify: (message: string, signature: string) =>
    request<{ token: string }>("/auth/verify", {
      method: "POST",
      body: JSON.stringify({ message, signature }),
    }),

  getMe: () => request<Me>("/auth/me"),

  getEvents: () => request<EventSummary[]>("/events"),

  getEvent: (id: string) => request<EventDetail>(`/events/${id}`),

  getEventActivity: (id: string) => request<ChainActivity[]>(`/events/${id}/activity`),

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

  confirm: (orderId: string, payment?: PaymentAuthorization) =>
    request<{ orderId: string; mintJobsCreated: number; paidOnChain: boolean }>("/checkout/confirm", {
      method: "POST",
      body: JSON.stringify({ orderId, payment }),
    }),

  getTickets: (token?: string) => request<TicketSummary[]>("/tickets", { token }),

  getVoucher: (orderId: string) => request<VoucherResponse>(`/orders/${orderId}/voucher`),

  getChainHealth: () => request<ChainHealth>("/health/chain"),

  /** Fetched once per page load; a failed request is retried on the next call. */
  getChainConfig: () => {
    chainConfigPromise ??= request<ChainConfig>("/chain/config").catch((err) => {
      chainConfigPromise = null;
      throw err;
    });
    return chainConfigPromise;
  },

  requestChallenge: (ticketId: string) =>
    request<{ ticketId: string; challenge: string; expiresInSeconds: number; eventName: string; tierName: string }>(
      `/redeem/${ticketId}/challenge`,
      { method: "POST" }
    ),

  confirmRedeem: (ticketId: string, challenge: string, signature: string) =>
    request<{ success: boolean; ticketId: string; redeemedAt: string; eventName: string; tierName: string; checkedIn: number; sold: number }>(`/redeem/${ticketId}/confirm`, {
      method: "POST",
      body: JSON.stringify({ challenge, signature }),
    }),

  // ----- staff ---------------------------------------------------------

  getScannableEvents: () => request<{ scopedToAssignments: boolean; events: ScannableEvent[] }>("/redeem/events"),

  // ----- admin ---------------------------------------------------------

  getUsers: (search?: string) =>
    request<AdminUser[]>(`/admin/users${search ? `?search=${encodeURIComponent(search)}` : ""}`),

  grantRole: (wallet: string, role: Role) =>
    request<{ message: string }>(`/admin/users/${wallet}/roles`, { method: "POST", body: JSON.stringify({ role }) }),

  revokeRole: (wallet: string, role: Role) =>
    request<{ message: string }>(`/admin/users/${wallet}/roles/${role}`, { method: "DELETE" }),

  assignStaff: (eventId: string, wallet: string) =>
    request<{ message: string }>(`/admin/events/${eventId}/staff/${wallet}`, { method: "PUT" }),

  unassignStaff: (eventId: string, wallet: string) =>
    request<{ message: string }>(`/admin/events/${eventId}/staff/${wallet}`, { method: "DELETE" }),

  cancelEvent: (eventId: string) =>
    request<{ message: string; settlesOnChain: boolean }>(`/admin/events/${eventId}/cancel`, { method: "POST" }),

  getAuditLog: () => request<AuditEntry[]>("/admin/audit-log?limit=50"),

  /** Pre-seeded Admin and Staff wallets. Empty unless the backend runs with Demo:ExposeAccounts. */
  getDemoAccounts: () => request<DemoAccount[]>("/demo/accounts").catch(() => [] as DemoAccount[]),
};

export type Role = "User" | "Staff" | "Admin";

export interface Me {
  userId: string;
  wallet: string;
  roles: Role[];
  staffEventIds: string[];
}

export interface DemoAccount {
  label: string;
  role: Role;
  privateKey: `0x${string}`;
}

export interface ScannableEvent {
  id: string;
  name: string;
  venueName: string;
  startsAt: string;
  sold: number;
  checkedIn: number;
}

export interface AdminUser {
  id: string;
  wallet: string;
  roles: Role[];
  staffEventIds: string[];
  createdAt: string;
  hasSignedIn: boolean;
  configuredAdmin: boolean;
}

export interface AuditEntry {
  id: number;
  actorWallet: string;
  action: string;
  target: string;
  details: string | null;
  createdAt: string;
}

export interface ChainHealth {
  status: "Healthy" | "Simulated" | "Degraded";
  message: string;
  chainId?: number;
  headBlock?: number | null;
  pendingMints?: number;
}

export type ChainConfig =
  | { enabled: false }
  | {
      enabled: true;
      chainId: number;
      rpcUrl: string;
      explorerUrl: string | null;
      contracts: {
        ticketFactory: `0x${string}`;
        eventEscrow: `0x${string}`;
        priceCappedResale: `0x${string}`;
        usdc: `0x${string}`;
        signatureValidator: `0x${string}`;
      };
      voucherSigner: string;
      faucetEnabled: boolean;
    };

export type EnabledChainConfig = Extract<ChainConfig, { enabled: true }>;

/** EIP-3009 receiveWithAuthorization parameters signed by the buyer. */
export interface PaymentAuthorization {
  validAfter: number;
  validBefore: number;
  nonce: `0x${string}`;
  signature: `0x${string}`;
}

export interface VoucherResponse {
  chainId: number;
  ticketContract: `0x${string}`;
  escrow: `0x${string}`;
  usdc: `0x${string}`;
  voucher: { buyer: `0x${string}`; tierId: string; quantity: string; nonce: string; deadline: string };
  signature: `0x${string}`;
  unitPrice: string;
  payment: { validAfter: string; validBefore: string; nonce: `0x${string}`; signature: `0x${string}` } | null;
}

export interface ChainActivity {
  kind: string;
  summary: string;
  txHash: string;
  blockNumber: number;
  contractAddress: string;
  createdAt: string;
}

export interface TierSummary {
  id: string;
  name: string;
  priceUsdc: number;
  totalSupply: number;
  maxPerWallet: number;
  ticketsSold: number;
}

export interface TierDetail extends TierSummary {
  onChainTierId: number;
  redeemedCount: number;
}

export interface EventSummary {
  id: string;
  name: string;
  description: string;
  venueName: string;
  startsAt: string;
  endsAt: string;
  organizerId: string;
  contractAddress: `0x${string}` | null;
  cancelledAt: string | null;
  tiers: TierSummary[];
}

export interface EventDetail extends Omit<EventSummary, "tiers"> {
  organizerWallet: string;
  deployTxHash: string | null;
  cancelRequestedAt: string | null;
  cancelTxHash: string | null;
  escrowReleasedAt: string | null;
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
  contractAddress: `0x${string}` | null;
  eventCancelledAt: string | null;
  orderStatus: "Pending" | "Confirmed" | string;
  paidOnChain: boolean;
  mintTxHash: string | null;
  mintStatus: "Pending" | "Submitted" | "Confirmed" | "Failed" | null;
  mintError: string | null;
  redeemedAt: string | null;
  checkpointedAt: string | null;
  badgeClaimedAt: string | null;
  refundedAt: string | null;
  createdAt: string;
}

export interface CreateEventPayload {
  name: string;
  description: string;
  venueName: string;
  startsAt: string;
  endsAt?: string;
  tiers: { name: string; priceUsdc: number; totalSupply: number; maxPerWallet?: number }[];
}
