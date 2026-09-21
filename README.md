# base-ticketing

Ticketing application built for the Base L2 network.

## Project Structure

```
base-ticketing/
├── TixFlow.sln                   ← .NET solution
├── docker-compose.yml            ← Postgres + Redis for local dev
├── src/
│   ├── contracts/                ← Foundry (Solidity smart contracts)
│   ├── backend/                  ← ASP.NET Core
│   │   └── TixFlow/
│   │       ├── TixFlow.Api/
│   │       ├── TixFlow.Domain/
│   │       ├── TixFlow.Infrastructure/
│   │       ├── TixFlow.Workers/
│   │       └── TixFlow.Tests/
│   └── frontend/                 ← React / Next.js
│       └── apps/
│           ├── buyer/
│           └── organizer/
└── scripts/                      ← Dev scripts, k6 load tests
```

## Getting Started

```bash
# Start local Postgres + Redis
docker compose up -d

# Apply EF Core migrations
dotnet ef database update \
  --project src/backend/TixFlow/TixFlow.Infrastructure/TixFlow.Infrastructure.csproj \
  --startup-project src/backend/TixFlow/TixFlow.Api/TixFlow.Api.csproj

# Run the API (http://localhost:5222)
dotnet run --project src/backend/TixFlow/TixFlow.Api

# Run tests
dotnet test
```

### Local Dev Credentials (docker-compose)

| Service  | Host      | Port | User     | Password     | Database |
|----------|-----------|------|----------|--------------|----------|
| Postgres | localhost | 5432 | tixflow  | tixflow_dev  | tixflow  |
| Redis    | localhost | 6379 | —        | —            | —        |

## Authentication (SIWE)

The API uses Sign-In with Ethereum (EIP-4361). The flow:

1. **GET /auth/nonce** — returns a single-use nonce (5 min TTL)
2. **POST /auth/verify** — accepts `{ message, signature }`, verifies the
   SIWE message signature, find-or-creates a User by wallet address, returns
   a JWT (24h expiry)
3. **GET /auth/me** — returns the authenticated user's id and wallet
   (requires `Authorization: Bearer <token>`)

See [TixFlow.http](src/backend/TixFlow/TixFlow.Api/TixFlow.http) for a
step-by-step manual test flow using the VS Code REST Client or Rider.

### Quick test with ethers.js

```js
import { Wallet } from "ethers";
const wallet = new Wallet("<PRIVATE_KEY>");

// 1. Get nonce
const { nonce } = await fetch("http://localhost:5222/auth/nonce").then(r => r.json());

// 2. Build & sign SIWE message
const message = `localhost wants you to sign in with your Ethereum account:
${wallet.address}

Sign in to TixFlow

URI: http://localhost:5222
Version: 1
Chain ID: 8453
Nonce: ${nonce}
Issued At: ${new Date().toISOString()}`;

const signature = await wallet.signMessage(message);

// 3. Verify
const { token } = await fetch("http://localhost:5222/auth/verify", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ message, signature }),
}).then(r => r.json());

// 4. Access protected endpoint
const me = await fetch("http://localhost:5222/auth/me", {
  headers: { Authorization: `Bearer ${token}` },
}).then(r => r.json());
console.log(me);
```
