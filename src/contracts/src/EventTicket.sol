// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {ERC721Upgradeable} from "@openzeppelin/contracts-upgradeable/token/ERC721/ERC721Upgradeable.sol";
import {ERC2981Upgradeable} from "@openzeppelin/contracts-upgradeable/token/common/ERC2981Upgradeable.sol";
import {AccessControlDefaultAdminRulesUpgradeable} from
    "@openzeppelin/contracts-upgradeable/access/extensions/AccessControlDefaultAdminRulesUpgradeable.sol";
import {PausableUpgradeable} from "@openzeppelin/contracts-upgradeable/utils/PausableUpgradeable.sol";
import {EIP712Upgradeable} from "@openzeppelin/contracts-upgradeable/utils/cryptography/EIP712Upgradeable.sol";
import {SignatureChecker} from "@openzeppelin/contracts/utils/cryptography/SignatureChecker.sol";
import {MerkleProof} from "@openzeppelin/contracts/utils/cryptography/MerkleProof.sol";

import {IEventEscrow} from "./interfaces/IEventEscrow.sol";
import {ITicketRenderer} from "./interfaces/ITicketRenderer.sol";
import {MintVoucher, MintOrder, TierInit, TicketInit, RenderParams} from "./TixFlowTypes.sol";

/// @title EventTicket
/// @notice ERC-721 tickets for one event. Deployed as an EIP-1167 clone by {TicketFactory}.
/// @dev Security properties enforced here rather than in the backend:
///  - per-tier supply caps and per-wallet purchase limits, so calling the contract directly gains nothing;
///  - a transfer lock until `unlockTime`, with the price-capped marketplace as the only exception;
///  - order nonces shared by both mint paths, so an order can never be minted twice;
///  - redeemed tickets are soulbound attendance badges.
contract EventTicket is
    ERC721Upgradeable,
    ERC2981Upgradeable,
    AccessControlDefaultAdminRulesUpgradeable,
    PausableUpgradeable,
    EIP712Upgradeable
{
    /// @notice May mint paid-off-chain or complimentary orders in batches (the backend mint worker).
    bytes32 public constant MINTER_ROLE = keccak256("MINTER_ROLE");
    /// @notice May publish Merkle roots of tickets redeemed at the door.
    bytes32 public constant REDEEMER_ROLE = keccak256("REDEEMER_ROLE");
    /// @notice May pause and unpause minting and transfers.
    bytes32 public constant PAUSER_ROLE = keccak256("PAUSER_ROLE");

    bytes32 public constant MINT_VOUCHER_TYPEHASH =
        keccak256("MintVoucher(address buyer,uint256 tierId,uint256 quantity,uint256 nonce,uint256 deadline)");

    /// @notice Why a batch order was skipped instead of minted.
    enum MintRejection {
        None,
        NonceUsed,
        ZeroAddress,
        ZeroQuantity,
        UnknownTier,
        SupplyCapExceeded,
        MaxPerWalletExceeded
    }

    struct Tier {
        uint128 price;
        uint32 supplyCap;
        uint32 minted;
        uint32 maxPerWallet;
        bool exists;
        string name;
    }

    struct Ticket {
        uint64 tierId;
        bool redeemed;
    }

    /// @notice Receives primary-sale proceeds (via escrow) and resale royalties.
    address public organizer;
    /// @notice The only address allowed to move tickets while the transfer lock is active.
    address public resaleContract;
    /// @notice Escrow that holds primary-sale payments for this event.
    address public escrow;
    /// @notice Contract that renders token metadata on-chain.
    address public renderer;
    /// @notice Signer of mint vouchers. May be an EOA or an ERC-1271 contract.
    address public voucherSigner;

    uint64 public eventStart;
    uint64 public eventEnd;
    /// @notice Timestamp after which unredeemed tickets become freely transferable.
    uint64 public unlockTime;

    /// @notice Latest Merkle root over every ticket redeemed at the door so far.
    bytes32 public redemptionRoot;
    /// @notice Number of leaves covered by `redemptionRoot`.
    uint256 public redemptionCount;
    /// @notice Number of roots published.
    uint256 public checkpointCount;
    /// @notice Number of attendance badges claimed on-chain.
    uint256 public badgesClaimed;

    uint256 private _nextTokenId;

    mapping(uint256 tierId => Tier) private _tiers;
    mapping(uint256 tokenId => Ticket) private _tickets;
    /// @notice Tickets of a tier ever minted to a wallet. Enforces `maxPerWallet`.
    mapping(uint256 tierId => mapping(address wallet => uint256)) public mintedBy;
    /// @notice Order nonces already consumed for a buyer (replay protection for both mint paths).
    mapping(address buyer => mapping(uint256 nonce => bool)) public nonceUsed;

    event Minted(
        address indexed buyer, uint256 indexed tierId, uint256 indexed nonce, uint256 firstTokenId, uint256 quantity
    );
    event OrderRejected(address indexed buyer, uint256 indexed nonce, MintRejection reason);
    event Redeemed(uint256 indexed tokenId, address indexed holder);
    event RedemptionsCheckpointed(bytes32 indexed root, uint256 count, uint256 indexed checkpointId);
    event TierConfigured(uint256 indexed tierId, string name, uint256 price, uint256 supplyCap, uint256 maxPerWallet);
    event UnlockTimeSet(uint64 unlockTime);
    event VoucherSignerSet(address indexed signer);
    event ResaleContractSet(address indexed resaleContract);

    error ZeroAddress();
    error InvalidSchedule();
    error InvalidTierConfig();
    error UnknownTier(uint256 tierId);
    error ZeroQuantity();
    error SupplyCapExceeded(uint256 tierId);
    error MaxPerWalletExceeded(uint256 tierId, address wallet);
    error VoucherExpired(uint256 deadline);
    error VoucherAlreadyUsed(address buyer, uint256 nonce);
    error InvalidVoucherSignature();
    error TransferLocked(uint256 tokenId, uint64 unlockTime);
    error Soulbound(uint256 tokenId);
    error AlreadyRedeemed(uint256 tokenId);
    error InvalidProof();
    error InvalidCheckpoint();
    error TicketListed(uint256 tokenId);

    /// @custom:oz-upgrades-unsafe-allow constructor
    constructor() {
        _disableInitializers();
    }

    /// @notice Initialises a freshly cloned ticket contract. Callable once.
    /// @param p Event metadata, role holders, linked contracts, schedule and tier configuration.
    function initialize(TicketInit calldata p) external initializer {
        if (p.organizer == address(0) || p.escrow == address(0) || p.renderer == address(0)) revert ZeroAddress();
        if (p.eventStart >= p.eventEnd) revert InvalidSchedule();

        __ERC721_init(p.name, p.symbol);
        __ERC2981_init();
        __AccessControlDefaultAdminRules_init(p.adminDelay, p.admin);
        __Pausable_init();
        __EIP712_init("TixFlow EventTicket", "1");

        organizer = p.organizer;
        escrow = p.escrow;
        renderer = p.renderer;
        resaleContract = p.resaleContract;
        voucherSigner = p.voucherSigner;
        eventStart = p.eventStart;
        eventEnd = p.eventEnd;
        unlockTime = p.unlockTime;
        _nextTokenId = 1;

        _setDefaultRoyalty(p.organizer, p.royaltyBps);

        if (p.minter != address(0)) _grantRole(MINTER_ROLE, p.minter);
        if (p.redeemer != address(0)) _grantRole(REDEEMER_ROLE, p.redeemer);
        if (p.pauser != address(0)) _grantRole(PAUSER_ROLE, p.pauser);

        uint256 length = p.tiers.length;
        for (uint256 i = 0; i < length; ++i) {
            TierInit calldata t = p.tiers[i];
            _configureTier(t.tierId, t.name, t.price, t.supplyCap, t.maxPerWallet);
        }

        emit UnlockTimeSet(p.unlockTime);
        emit VoucherSignerSet(p.voucherSigner);
        emit ResaleContractSet(p.resaleContract);
    }

    // ---------------------------------------------------------------------
    // Minting
    // ---------------------------------------------------------------------

    /// @notice Mints a batch of orders paid off-chain or issued free of charge.
    /// @dev An order that cannot be minted is skipped with {OrderRejected} instead of reverting, so one bad
    ///      order (or one already self-claimed with a voucher) cannot block the rest of the batch.
    /// @param orders Orders to mint; each consumes its `(buyer, nonce)` pair.
    function mintBatch(MintOrder[] calldata orders) external onlyRole(MINTER_ROLE) whenNotPaused {
        uint256 length = orders.length;
        for (uint256 i = 0; i < length; ++i) {
            MintOrder calldata o = orders[i];
            MintRejection reason =
                nonceUsed[o.buyer][o.nonce] ? MintRejection.NonceUsed : _checkMint(o.buyer, o.tierId, o.quantity);
            if (reason != MintRejection.None) {
                emit OrderRejected(o.buyer, o.nonce, reason);
                continue;
            }
            nonceUsed[o.buyer][o.nonce] = true;
            _mintTickets(o.buyer, o.tierId, o.quantity, o.nonce);
        }
    }

    /// @notice Mints tickets authorised by a backend-signed EIP-712 voucher and collects payment into escrow.
    /// @dev Anyone may submit the voucher; tickets always go to `voucher.buyer` and `msg.sender` pays.
    ///      This is the path a buyer uses to claim a purchase themselves if the mint worker is unavailable.
    /// @param voucher The signed mint authorisation.
    /// @param signature Signature by `voucherSigner` (EOA signature or ERC-1271 contract signature).
    /// @return firstTokenId The first of `voucher.quantity` consecutive token IDs minted.
    function mintWithVoucher(MintVoucher calldata voucher, bytes calldata signature)
        external
        whenNotPaused
        returns (uint256 firstTokenId)
    {
        if (block.timestamp > voucher.deadline) revert VoucherExpired(voucher.deadline);
        if (nonceUsed[voucher.buyer][voucher.nonce]) revert VoucherAlreadyUsed(voucher.buyer, voucher.nonce);
        if (!SignatureChecker.isValidSignatureNow(voucherSigner, hashVoucher(voucher), signature)) {
            revert InvalidVoucherSignature();
        }

        MintRejection reason = _checkMint(voucher.buyer, voucher.tierId, voucher.quantity);
        if (reason != MintRejection.None) _revertWith(reason, voucher.tierId, voucher.buyer);

        nonceUsed[voucher.buyer][voucher.nonce] = true;
        uint256 unitPrice = _tiers[voucher.tierId].price;
        firstTokenId = _mintTickets(voucher.buyer, voucher.tierId, voucher.quantity, voucher.nonce);

        if (unitPrice != 0) {
            IEventEscrow(escrow).collectPayment(msg.sender, voucher.buyer, firstTokenId, voucher.quantity, unitPrice);
        }
    }

    // ---------------------------------------------------------------------
    // Redemption and proof of attendance
    // ---------------------------------------------------------------------

    /// @notice Publishes the Merkle root of every ticket redeemed at the door so far.
    /// @dev Door entry is verified off-chain and never waits for this call. Each root is cumulative.
    /// @param root Root of a tree whose leaves are `keccak256(bytes.concat(keccak256(abi.encode(tokenId))))`.
    /// @param count Number of redeemed tickets in the tree; must not decrease.
    function checkpointRedemptions(bytes32 root, uint256 count) external onlyRole(REDEEMER_ROLE) {
        if (root == bytes32(0) || count < redemptionCount || count > _nextTokenId - 1) revert InvalidCheckpoint();
        redemptionRoot = root;
        redemptionCount = count;
        emit RedemptionsCheckpointed(root, count, ++checkpointCount);
    }

    /// @notice Marks a checkpointed ticket as redeemed, turning it into a soulbound attendance badge.
    /// @dev Permissionless: the proof is the authorisation, so a holder, the backend or a wary resale buyer
    ///      can all trigger it.
    /// @param tokenId The redeemed ticket.
    /// @param proof Merkle proof of `tokenId` against `redemptionRoot`.
    function claimAttendanceBadge(uint256 tokenId, bytes32[] calldata proof) external {
        address holder = _requireOwned(tokenId);
        if (_tickets[tokenId].redeemed) revert AlreadyRedeemed(tokenId);
        if (holder == resaleContract) revert TicketListed(tokenId);

        bytes32 leaf = keccak256(bytes.concat(keccak256(abi.encode(tokenId))));
        if (!MerkleProof.verifyCalldata(proof, redemptionRoot, leaf)) revert InvalidProof();

        _tickets[tokenId].redeemed = true;
        unchecked {
            ++badgesClaimed;
        }
        emit Redeemed(tokenId, holder);
    }

    // ---------------------------------------------------------------------
    // Administration
    // ---------------------------------------------------------------------

    /// @notice Creates or updates a tier. The supply cap can never drop below what is already minted.
    /// @param tierId Tier identifier.
    /// @param tierName Display name used in token metadata.
    /// @param price Unit price in the payment token's smallest unit.
    /// @param supplyCap Maximum number of tickets in the tier.
    /// @param maxPerWallet Maximum tickets one wallet may mint in the tier; 0 means unlimited.
    function configureTier(
        uint256 tierId,
        string calldata tierName,
        uint256 price,
        uint256 supplyCap,
        uint256 maxPerWallet
    ) external onlyRole(DEFAULT_ADMIN_ROLE) {
        _configureTier(tierId, tierName, price, supplyCap, maxPerWallet);
    }

    /// @notice Sets when unredeemed tickets become freely transferable.
    /// @param newUnlockTime Unix timestamp.
    function setUnlockTime(uint64 newUnlockTime) external onlyRole(DEFAULT_ADMIN_ROLE) {
        unlockTime = newUnlockTime;
        emit UnlockTimeSet(newUnlockTime);
    }

    /// @notice Rotates the voucher signer, e.g. after a key compromise.
    /// @param signer New signer; the zero address disables voucher minting.
    function setVoucherSigner(address signer) external onlyRole(DEFAULT_ADMIN_ROLE) {
        // slither-disable-next-line missing-zero-check
        voucherSigner = signer;
        emit VoucherSignerSet(signer);
    }

    /// @notice Sets the marketplace exempt from the transfer lock.
    /// @param newResaleContract Marketplace address; the zero address disables resale.
    function setResaleContract(address newResaleContract) external onlyRole(DEFAULT_ADMIN_ROLE) {
        // slither-disable-next-line missing-zero-check
        resaleContract = newResaleContract;
        emit ResaleContractSet(newResaleContract);
    }

    /// @notice Pauses minting and all transfers.
    function pause() external onlyRole(PAUSER_ROLE) {
        _pause();
    }

    /// @notice Resumes minting and transfers.
    function unpause() external onlyRole(PAUSER_ROLE) {
        _unpause();
    }

    // ---------------------------------------------------------------------
    // Views
    // ---------------------------------------------------------------------

    /// @notice EIP-712 digest a voucher must be signed over.
    /// @param voucher The voucher to hash.
    function hashVoucher(MintVoucher calldata voucher) public view returns (bytes32) {
        return _hashTypedDataV4(
            keccak256(
                abi.encode(
                    MINT_VOUCHER_TYPEHASH,
                    voucher.buyer,
                    voucher.tierId,
                    voucher.quantity,
                    voucher.nonce,
                    voucher.deadline
                )
            )
        );
    }

    /// @notice Full configuration and mint count of a tier.
    /// @param tierId Tier identifier.
    function tierConfig(uint256 tierId)
        external
        view
        returns (string memory tierName, uint256 price, uint256 supplyCap, uint256 minted, uint256 maxPerWallet)
    {
        Tier storage t = _tiers[tierId];
        if (!t.exists) revert UnknownTier(tierId);
        return (t.name, t.price, t.supplyCap, t.minted, t.maxPerWallet);
    }

    /// @notice Unit price (face value) of a tier.
    /// @param tierId Tier identifier.
    function tierPrice(uint256 tierId) external view returns (uint256) {
        Tier storage t = _tiers[tierId];
        if (!t.exists) revert UnknownTier(tierId);
        return t.price;
    }

    /// @notice Tier a ticket belongs to.
    /// @param tokenId Ticket identifier.
    function tierOf(uint256 tokenId) external view returns (uint256) {
        _requireOwned(tokenId);
        return _tickets[tokenId].tierId;
    }

    /// @notice Whether a ticket has been redeemed on-chain (and is therefore soulbound).
    /// @param tokenId Ticket identifier.
    function isRedeemed(uint256 tokenId) external view returns (bool) {
        return _tickets[tokenId].redeemed;
    }

    /// @notice Number of tickets minted across all tiers.
    function totalMinted() external view returns (uint256) {
        return _nextTokenId - 1;
    }

    /// @notice Whether `tokenId` could currently be transferred by `operator`.
    /// @param tokenId Ticket identifier.
    /// @param operator Address that would call the transfer.
    function isTransferable(uint256 tokenId, address operator) external view returns (bool) {
        if (paused() || _tickets[tokenId].redeemed) return false;
        return operator == resaleContract || block.timestamp >= unlockTime;
    }

    /// @notice Fully on-chain metadata: base64 JSON embedding a base64 SVG of the ticket.
    /// @param tokenId Ticket identifier.
    function tokenURI(uint256 tokenId) public view override returns (string memory) {
        _requireOwned(tokenId);
        Ticket storage ticket = _tickets[tokenId];
        return ITicketRenderer(renderer).tokenURI(
            RenderParams({
                eventName: name(),
                tierName: _tiers[ticket.tierId].name,
                tokenId: tokenId,
                tierId: ticket.tierId,
                eventStart: eventStart,
                redeemed: ticket.redeemed
            })
        );
    }

    /// @inheritdoc ERC721Upgradeable
    function supportsInterface(bytes4 interfaceId)
        public
        view
        override(ERC721Upgradeable, ERC2981Upgradeable, AccessControlDefaultAdminRulesUpgradeable)
        returns (bool)
    {
        return super.supportsInterface(interfaceId);
    }

    // ---------------------------------------------------------------------
    // Internals
    // ---------------------------------------------------------------------

    function _configureTier(
        uint256 tierId,
        string calldata tierName,
        uint256 price,
        uint256 supplyCap,
        uint256 maxPerWallet
    ) private {
        Tier storage t = _tiers[tierId];
        if (
            tierId > type(uint64).max || price > type(uint128).max || supplyCap > type(uint32).max
                || maxPerWallet > type(uint32).max || supplyCap < t.minted
        ) revert InvalidTierConfig();

        t.price = uint128(price);
        t.supplyCap = uint32(supplyCap);
        t.maxPerWallet = uint32(maxPerWallet);
        t.exists = true;
        t.name = tierName;
        emit TierConfigured(tierId, tierName, price, supplyCap, maxPerWallet);
    }

    function _checkMint(address to, uint256 tierId, uint256 quantity) private view returns (MintRejection) {
        if (to == address(0)) return MintRejection.ZeroAddress;
        if (quantity == 0) return MintRejection.ZeroQuantity;
        Tier storage t = _tiers[tierId];
        if (!t.exists) return MintRejection.UnknownTier;
        if (quantity > uint256(t.supplyCap) - t.minted) return MintRejection.SupplyCapExceeded;
        if (t.maxPerWallet != 0 && mintedBy[tierId][to] + quantity > t.maxPerWallet) {
            return MintRejection.MaxPerWalletExceeded;
        }
        return MintRejection.None;
    }

    function _revertWith(MintRejection reason, uint256 tierId, address wallet) private pure {
        if (reason == MintRejection.ZeroAddress) revert ZeroAddress();
        if (reason == MintRejection.ZeroQuantity) revert ZeroQuantity();
        if (reason == MintRejection.UnknownTier) revert UnknownTier(tierId);
        if (reason == MintRejection.SupplyCapExceeded) revert SupplyCapExceeded(tierId);
        revert MaxPerWalletExceeded(tierId, wallet);
    }

    /// @dev Uses `_mint`, not `_safeMint`: a receiver hook would let one buyer contract revert a whole
    ///      batch or re-enter the mint. Callers must have run `_checkMint` first.
    function _mintTickets(address to, uint256 tierId, uint256 quantity, uint256 nonce)
        private
        returns (uint256 firstTokenId)
    {
        firstTokenId = _nextTokenId;
        _nextTokenId = firstTokenId + quantity;
        _tiers[tierId].minted += uint32(quantity);
        mintedBy[tierId][to] += quantity;

        for (uint256 i = 0; i < quantity; ++i) {
            uint256 tokenId = firstTokenId + i;
            _tickets[tokenId].tierId = uint64(tierId);
            _mint(to, tokenId);
        }
        emit Minted(to, tierId, nonce, firstTokenId, quantity);
    }

    /// @dev Transfer rules. Mints and all transfers respect the pause. For transfers between holders:
    ///      redeemed tickets never move; otherwise only the marketplace may move a ticket before `unlockTime`.
    function _update(address to, uint256 tokenId, address auth) internal override returns (address) {
        _requireNotPaused();
        address from = _ownerOf(tokenId);
        if (from != address(0) && to != address(0)) {
            if (_tickets[tokenId].redeemed) revert Soulbound(tokenId);
            if (msg.sender != resaleContract && block.timestamp < unlockTime) {
                revert TransferLocked(tokenId, unlockTime);
            }
        }
        return super._update(to, tokenId, auth);
    }
}
