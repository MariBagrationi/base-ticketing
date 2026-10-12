// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

/// @notice Backend-signed EIP-712 authorisation to mint `quantity` tickets of `tierId` to `buyer`.
/// @dev `nonce` is unique per order and is burned on use, whichever mint path consumes it first.
struct MintVoucher {
    address buyer;
    uint256 tierId;
    uint256 quantity;
    uint256 nonce;
    uint256 deadline;
}

/// @notice One order in a minter-submitted batch. Shares the nonce space with {MintVoucher}.
struct MintOrder {
    address buyer;
    uint256 tierId;
    uint256 quantity;
    uint256 nonce;
}

/// @notice Tier configuration supplied when an event is created.
struct TierInit {
    uint256 tierId;
    string name;
    uint256 price;
    uint256 supplyCap;
    uint256 maxPerWallet;
}

/// @notice Initialisation parameters for an {EventTicket} clone.
struct TicketInit {
    string name;
    string symbol;
    address admin;
    uint48 adminDelay;
    address organizer;
    address minter;
    address redeemer;
    address pauser;
    address voucherSigner;
    address resaleContract;
    address escrow;
    address renderer;
    uint64 eventStart;
    uint64 eventEnd;
    uint64 unlockTime;
    uint96 royaltyBps;
    TierInit[] tiers;
}

/// @notice Data the renderer needs to draw a ticket.
struct RenderParams {
    string eventName;
    string tierName;
    uint256 tokenId;
    uint256 tierId;
    uint64 eventStart;
    bool redeemed;
}

/// @notice EIP-3009 `receiveWithAuthorization` parameters signed by the payer.
struct PaymentAuthorization {
    uint256 validAfter;
    uint256 validBefore;
    bytes32 nonce;
    uint8 v;
    bytes32 r;
    bytes32 s;
}

/// @notice EIP-2612 permit signature.
struct PermitSignature {
    uint256 deadline;
    uint8 v;
    bytes32 r;
    bytes32 s;
}
