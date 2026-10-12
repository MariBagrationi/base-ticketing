# TixFlow threat model

This document covers the on-chain protocol in `src/contracts` and the backend components that hold keys
or decide who may mint. It lists what is worth attacking, who might attack it, and for each scenario the
exact mechanism that stops it and the test that demonstrates it.

Test names refer to files in `src/contracts/test` unless a `.cs` file is given.

## 1. Assets

| Asset | Where it lives | Why it matters |
|-------|----------------|----------------|
| Primary-sale USDC | `EventEscrow` | Buyers' money until the event ends |
| Tickets | `EventTicket` clones (ERC-721) | Entry rights; scarce by design |
| Fair allocation | Waiting room + per-wallet limits | The product's core promise against bots and scalpers |
| Resale price cap | `PriceCappedResale` | Removes the profit motive for scalping |
| Proof of attendance | `redeemed` flag + Merkle root in `EventTicket` | Must not be forgeable or transferable |
| Relayer key | Workers process (`Chain:RelayerPrivateKey`) | Holds MINTER, REDEEMER, EVENT_CREATOR, CANCELLER |
| Voucher signer key | API process (`Chain:VoucherSignerPrivateKey`) | Authorises mints |
| Admin key | Offline / multisig (`DEFAULT_ADMIN_ROLE`) | Can reconfigure tiers, signer, pause |

## 2. Actors and trust assumptions

| Actor | Trusted for | Not trusted for |
|-------|-------------|-----------------|
| Buyer | Nothing | — |
| Scalper / bot operator | Nothing; can call any contract directly | — |
| Organizer | Running the event | Taking buyers' money before the event happens |
| TixFlow backend | Deciding who was admitted by the queue | Custody of funds; unbounded minting |
| Relayer account | Submitting transactions | Moving funds to itself (it cannot) |
| Admin (multisig) | Configuration, emergency pause | Instant takeover (transfers are two-step and delayed) |
| Base sequencer | Ordering and liveness | — (see sequencer outage) |

USDC is treated as a correct ERC-20 with EIP-3009 and EIP-2612. The fork test
`BaseSepoliaFork.t.sol` runs the purchase, refund and release flows against the real USDC contract on
Base Sepolia.

## 3. Attack scenarios

### 3.1 Scalper bot calls the contract directly, skipping the queue

*Goal:* mint tickets without being admitted, or mint more than the limit.

| Mitigation | Where |
|------------|-------|
| Minting needs either `MINTER_ROLE` or a voucher signed by `voucherSigner` | `EventTicket.mintBatch`, `EventTicket.mintWithVoucher` |
| Per-wallet limit and supply cap are checked in the contract, on both mint paths | `EventTicket._checkMint` |
| A voucher is bound to one buyer, tier, quantity, contract and chain (EIP-712 domain) | `EventTicket.hashVoucher` |

Tests: `test_MintBatch_RevertWhen_NotMinter`, `test_MintWithVoucher_RevertWhen_WrongSigner`,
`test_MintWithVoucher_RevertWhen_SignedForAnotherEvent`, `test_MaxPerWallet_EnforcedAcrossBothMintPaths`,
`testFuzz_MaxPerWalletNeverExceeded`, `testFuzz_SupplyCapNeverExceeded`,
`invariant_MaxPerWalletNeverExceeded`, `invariant_MintedNeverExceedsSupplyCap`.

*Residual risk:* one person can use many wallets. The waiting room and rate limiting raise the cost;
on-chain limits cannot stop Sybil buyers by themselves.

### 3.2 Replayed or tampered voucher

*Goal:* mint the same order twice, or change the quantity or recipient.

| Mitigation | Where |
|------------|-------|
| `nonceUsed[buyer][nonce]` is set before minting and shared by both mint paths | `EventTicket.mintWithVoucher`, `EventTicket.mintBatch` |
| Any change to a signed field changes the digest | `EventTicket.hashVoucher` |
| Vouchers expire | `VoucherExpired` check in `mintWithVoucher` |
| The backend derives the nonce from the order ID, so one order always maps to one nonce | `ChainIds.OrderNonce` (C#) |

Tests: `test_MintWithVoucher_RevertWhen_Replayed`, `test_Nonce_SharedBetweenMintPaths`,
`testFuzz_VoucherReplayAlwaysFails`, `testFuzz_VoucherTamperingFails`, `testFuzz_ExpiredVoucherFails`,
`testFuzz_VoucherFromWrongKeyFails`; backend: `ChainPrimitivesTests.VoucherSignature_MatchesTheDigestTheContractComputes`,
`ChainFlowTests.MintWorker_DoesNotResubmitAnOrderTheBuyerAlreadyClaimed`.

The self-claim path exists on purpose: a buyer holding a voucher can mint from their own wallet if the
worker is down. Because both paths burn the same nonce, the worker and the buyer cannot both mint the
order. A batch containing an already-claimed order skips it (`OrderRejected(NonceUsed)`) instead of
reverting, so one buyer cannot block a batch.

### 3.3 Scalping on the secondary market

*Goal:* resell above face value.

| Mitigation | Where |
|------------|-------|
| Tickets cannot be transferred before `unlockTime`, except by the marketplace | `EventTicket._update` |
| Listing price is capped at face value plus markup; face value is read from the ticket contract | `PriceCappedResale.list`, `maxResalePrice` |
| The cap is checked again at settlement | `PriceCappedResale.buy` |
| The admin can never raise the markup above 50% | `MAX_MARKUP_BPS` |

Tests: `test_PrivateSaleAroundTheMarketplaceIsImpossible`, `test_List_RevertWhen_AboveCap`,
`test_Buy_RevertWhen_MarkupLoweredBelowListingPrice`, `testFuzz_ResalePriceCapNeverExceeded`.

*Residual risk:* an off-chain side payment plus an at-cap listing. The buyer of such a deal has no
guarantee of receiving the ticket, because anyone can buy the public listing first.

### 3.4 Malicious or failing organizer

*Goal:* collect ticket revenue and not hold the event.

| Mitigation | Where |
|------------|-------|
| Revenue goes to the escrow, not to the organizer | `EventEscrow.collectPayment` |
| Release is only possible after `eventEnd` | `EventEscrow.release` |
| If the event is cancelled, each ticket's price is refundable to its current holder, by anyone's call | `EventEscrow.cancelEvent`, `claimRefund`, `refundBatch` |
| Cancellation is impossible after funds were released, and release is impossible after cancellation | `AlreadyReleased`, `EventIsCancelled` |

Tests: `test_Release_PaysOrganizerAfterEventEnd`, `test_ClaimRefund_PaysFullPriceToHolder`,
`test_ClaimRefund_GoesToCurrentHolderAfterResale`, `test_CancelEvent_RevertWhen_FundsAlreadyReleased`,
`testFuzz_RefundEqualsPayment`, `invariant_EscrowBalanceMatchesUnsettledSales`.

*Residual risk:* an organizer who simply does not show up and does not cancel. The platform admin and
the backend's `CANCELLER_ROLE` can cancel on buyers' behalf, but only before `release` is called, and
`release` is permissionless after `eventEnd`. A dispute window between `eventEnd` and release is on the
roadmap.

### 3.5 Compromised relayer (minter) key

*Impact:* the attacker can mint tickets up to each tier's remaining supply and per-wallet limit, publish
a false redemption root, create events, and cancel events.

*What the attacker cannot do:* take any USDC (no function sends escrowed funds to the caller), exceed a
supply cap, or move existing tickets.

| Mitigation | Where |
|------------|-------|
| Minter is bound by the same caps as everyone else | `EventTicket._checkMint` |
| `PAUSER_ROLE` stops minting and transfers immediately | `EventTicket.pause` |
| Admin revokes the role and sets new role holders for future events | `revokeRole`, `TicketFactory.setPlatformConfig` |
| Cancelling only makes buyers refundable | `EventEscrow.cancelEvent` |

Tests: `testFuzz_MintBatchRespectsLimits`, `test_MintBatch_RevertWhen_Paused`, `test_CancelEvent_ByCancellerRole`.

### 3.6 Compromised voucher signer key

*Impact:* the attacker can sign vouchers and mint, paying full price into escrow for paid tiers.

| Mitigation | Where |
|------------|-------|
| Paid tiers still require payment; the signer cannot mint paid tickets for free | `EventTicket.mintWithVoucher` → `EventEscrow.collectPayment` |
| Caps still apply | `_checkMint` |
| Admin rotates the signer; setting it to zero disables voucher minting | `EventTicket.setVoucherSigner` |

Tests: `test_MintWithVoucher_RevertWhen_PayerHasNoAllowance`, `test_MintWithVoucher_RevertWhen_SignerDisabled`.

### 3.7 Stealing or redirecting a buyer's payment

*Goal:* use a buyer's signed USDC authorisation for something else.

| Mitigation | Where |
|------------|-------|
| The buyer signs `receiveWithAuthorization`, which USDC only honours when the caller is the payee, i.e. the escrow | EIP-3009, `EventEscrow.purchaseWithAuthorization` |
| The escrow pulls exactly `price × quantity`, computed from the ticket contract | `EventEscrow._quote` |
| Payment and mint are atomic; if the mint fails the payment reverts | `purchaseWithAuthorization` |
| A registered contract cannot make the escrow account for a different amount than it pulled | `_prepaid` / `PrepaidMismatch` |

Tests: `test_PurchaseWithAuthorization_MintsAndEscrowsAtomically`,
`test_PurchaseWithAuthorization_RevertWhen_AuthorizationForWrongAmount`,
`test_PurchaseWithAuthorization_RevertWhen_VoucherInvalid_AndPaymentIsRolledBack`,
`test_Purchase_RevertWhen_TicketSkipsPaymentAccounting`, `test_Purchase_RevertWhen_TicketAccountsWrongAmount`.

### 3.8 Forged or transferred proof of attendance

| Mitigation | Where |
|------------|-------|
| A badge needs a Merkle proof against a root only `REDEEMER_ROLE` can publish | `checkpointRedemptions`, `claimAttendanceBadge` |
| Leaves are double-hashed, which rules out second-preimage tricks with internal nodes | leaf encoding in `claimAttendanceBadge` |
| A redeemed ticket can never be transferred, even after unlock and even by the marketplace | `Soulbound` in `EventTicket._update` |
| A listed ticket cannot be redeemed, so nothing can get stuck in the marketplace | `TicketListed` |

Tests: `test_ClaimBadge_RevertWhen_InvalidProof`, `test_Transfer_RevertWhen_RedeemedEvenAfterUnlock`,
`test_ClaimBadge_RevertWhen_TicketIsListed`, `invariant_RedeemedTicketsNeverMove`; backend:
`ChainPrimitivesTests.MerkleTree_*`, and the end-to-end script, which has the contract verify a proof
built by the C# tree.

### 3.9 Re-entrancy and malicious receivers

| Mitigation | Where |
|------------|-------|
| Tickets are minted with `_mint`, not `_safeMint`, so no receiver hook runs during a mint or a batch | `EventTicket._mintTickets` |
| State changes precede external calls; fund-moving functions are `nonReentrant` | `EventEscrow`, `PriceCappedResale` |
| USDC has no transfer hooks | — |

### 3.10 Metadata injection

Event and tier names end up inside on-chain SVG and JSON. `TicketRenderer` escapes XML and JSON special
characters and strips control characters. Test: `test_Render_EscapesHostileNames`.

### 3.11 Admin key compromise or mistake

`AccessControlDefaultAdminRules` makes admin transfers two-step and delayed (1 day by default), with one
admin at a time. Test: `test_DefaultAdmin_TransferIsTwoStepAndDelayed`. The markup and protocol fee have
hard upper bounds in the contract.

### 3.12 Sequencer or RPC outage

No request in the checkout path calls the chain. Orders are confirmed in Postgres with the buyer's
signed payment authorisation and settled later by the worker. Chain health is reported from the
indexer's heartbeat, not from a live call. Outages do not count as failed mint attempts.

Tests: `ChainFlowTests.Checkout_Succeeds_WhileTheChainIsUnreachable`,
`ChainFlowTests.MintWorker_KeepsJobsPendingWithoutPenalty_WhenTheChainIsDown`,
`ChainFlowTests.ChainHealth_ReportsDegraded_WithoutCallingTheChain`.

*Residual risk:* Base has a single sequencer. During an outage tickets are not minted and payment
authorisations (valid for 24 hours) may expire, in which case the buyer has to sign again.

### 3.13 Indexer fed a reorganised chain

The indexer waits for confirmation blocks, stores the hash of the last processed block, and on a
mismatch rewinds and reverts recently confirmed mints until they are observed again. Log handlers are
idempotent. Tests: `IndexerTests.ReorgDetection_RevertsConfirmedJobsToSubmitted`,
`ChainFlowTests.Indexer_ReplayingALog_IsIdempotent`.

## 4. Static analysis (Slither)

Slither runs in CI (`.github/workflows/ci.yml`) with `src/contracts/slither.config.json` and fails the
build on any finding of low severity or above. Current result: **0 findings** at any severity. The first run reported 21; each was handled as
follows.

| Detector | Location | Disposition |
|----------|----------|-------------|
| `arbitrary-send-erc20` | `EventEscrow.collectPayment` | By design, annotated. `payer` is the caller of a registered ticket's `mintWithVoucher`; the function is only callable by registered ticket contracts. |
| `arbitrary-send-erc20-permit` | `EventEscrow.purchaseWithPermit` | By design, annotated. The pull is bounded by the buyer's own permit and requires a backend-signed voucher naming that buyer. |
| `uninitialized-local` (2) | `TicketRenderer` | Fixed: buffers are initialised explicitly. |
| `unused-return` | `SignatureValidator.isValidSig` | Annotated. The third return value of `tryRecover` is an error argument that is not needed. |
| `missing-zero-check` (4) | `setVoucherSigner`, `setResaleContract`, `PriceCappedResale` constructor, `SignatureValidator` | Annotated. Zero is a meaningful "disabled" value for the two setters; the constructor's admin is validated by the parent; the validator's target is an arbitrary ERC-6492 factory. |
| `reentrancy-events` | `TicketFactory.createEvent` | Fixed: the event is emitted before the external calls. |
| `missing-inheritance` | `PriceCappedResale` | Fixed: it now implements `IResaleRegistrar`. |
| `low-level-calls` | `SignatureValidator` | Annotated. ERC-6492 requires calling an arbitrary wallet factory; the contract holds no funds or roles. |
| `calls-loop`, `costly-loop` (4) | `EventEscrow.refundBatch`, `EventTicket.mintBatch` | Accepted; excluded in the config. Batch functions loop by nature; batch sizes are chosen by the caller, and `refundBatch` only ever calls trusted ticket contracts. |
| `timestamp` (5) | time-based rules | Accepted; excluded in the config. Deadlines, unlock time and event end are inherently time-based; a sequencer's few seconds of latitude do not change any outcome. |

## 5. Known limitations

- **Keys are read from environment variables**, not from a KMS or HSM. The relayer and voucher signer
  should move to a managed signer before any real money is involved.
- **No paymaster.** Buyers pay no gas because the relayer submits their signed authorisation; a buyer
  who self-claims pays their own gas. ERC-4337 sponsorship is on the roadmap.
- **Single sequencer dependency** (section 3.12).
- **Simplified stuck-transaction recovery.** A submitted transaction that is not seen within the timeout
  is resubmitted with a fresh nonce; there is no fee bumping of the original transaction.
- **Door redemption is trusted to the backend.** A dishonest backend could publish a root marking a
  ticket as attended. It cannot move the ticket or any funds.
- **A USDC-blacklisted organizer blocks `release`** for that event, because the payout is pushed. Funds
  stay in escrow rather than being lost.
- **The payment signature is not verified off-chain** at confirm time. An invalid one is caught when the
  worker simulates the transaction, and the order's mint is then marked failed.
- **Resale royalties and protocol fees are pull payments;** the seller's proceeds are pushed in the same
  transaction as the ticket transfer.
- The contracts have **not been audited** by a third party.
