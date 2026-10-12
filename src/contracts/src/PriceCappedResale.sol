// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {AccessControlDefaultAdminRules} from
    "@openzeppelin/contracts/access/extensions/AccessControlDefaultAdminRules.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import {SafeERC20} from "@openzeppelin/contracts/token/ERC20/utils/SafeERC20.sol";
import {IERC2981} from "@openzeppelin/contracts/interfaces/IERC2981.sol";

import {IEventTicket} from "./interfaces/IEventTicket.sol";
import {IEventEscrow} from "./interfaces/IEventEscrow.sol";
import {IResaleRegistrar} from "./interfaces/IResaleRegistrar.sol";

/// @title PriceCappedResale
/// @notice The only secondary market for TixFlow tickets. Listings are escrowed and can never be priced above
///         face value plus a capped markup, which removes the profit motive for scalping.
/// @dev {EventTicket} blocks every other transfer until its unlock time, so the cap cannot be bypassed with a
///      private sale. Settlement is atomic: the buyer's payment is split between seller, organizer royalty
///      (ERC-2981) and protocol fee in the same transaction that delivers the ticket.
contract PriceCappedResale is IResaleRegistrar, AccessControlDefaultAdminRules, ReentrancyGuard {
    using SafeERC20 for IERC20;

    /// @notice May register ticket contracts (the factory).
    bytes32 public constant REGISTRAR_ROLE = keccak256("REGISTRAR_ROLE");

    uint16 public constant BPS_DENOMINATOR = 10_000;
    /// @notice Upper bound the admin can ever set the markup to (50%).
    uint16 public constant MAX_MARKUP_BPS = 5_000;
    /// @notice Upper bound the admin can ever set the protocol fee to (10%).
    uint16 public constant MAX_PROTOCOL_FEE_BPS = 1_000;

    struct Listing {
        address seller;
        address ticket;
        uint256 tokenId;
        uint256 price;
        bool active;
    }

    /// @notice Payment token (USDC).
    IERC20 public immutable usdc;
    /// @notice Escrow consulted to block trading of cancelled events.
    IEventEscrow public immutable escrow;

    /// @notice Maximum markup over face value, in basis points.
    uint16 public markupBps;
    /// @notice Protocol fee taken from each sale, in basis points.
    uint16 public protocolFeeBps;
    /// @notice Recipient of protocol fees.
    address public feeRecipient;

    uint256 public nextListingId = 1;

    mapping(address ticket => bool) public isRegisteredTicket;
    mapping(uint256 listingId => Listing) public listings;
    /// @notice Royalties and fees waiting to be withdrawn (pull payments).
    mapping(address account => uint256) public pendingWithdrawals;

    event TicketRegistered(address indexed ticket);
    event Listed(
        uint256 indexed listingId, address indexed ticket, uint256 indexed tokenId, address seller, uint256 price
    );
    event Sold(
        uint256 indexed listingId,
        address indexed buyer,
        address indexed seller,
        uint256 price,
        uint256 royalty,
        uint256 protocolFee
    );
    event ListingCancelled(uint256 indexed listingId);
    event Withdrawn(address indexed account, uint256 amount);
    event MarkupSet(uint16 markupBps);
    event ProtocolFeeSet(uint16 protocolFeeBps, address indexed feeRecipient);

    error ZeroAddress();
    error AlreadyRegistered(address ticket);
    error NotRegistered(address ticket);
    error NotTicketOwner();
    error ZeroPrice();
    error PriceAboveCap(uint256 price, uint256 cap);
    error ResaleClosed();
    error TicketRedeemed(uint256 tokenId);
    error ListingNotActive(uint256 listingId);
    error NotSeller();
    error InvalidBps();
    error NothingToWithdraw();

    /// @param usdc_ Payment token.
    /// @param escrow_ Primary-sale escrow.
    /// @param admin Default admin (two-step, delayed transfer).
    /// @param adminDelay Delay applied to admin transfers, in seconds.
    /// @param markupBps_ Initial maximum markup over face value.
    /// @param registrar Initial holder of {REGISTRAR_ROLE} (the factory); zero to grant it later.
    constructor(
        IERC20 usdc_,
        IEventEscrow escrow_,
        address admin,
        uint48 adminDelay,
        uint16 markupBps_,
        address registrar
    ) AccessControlDefaultAdminRules(adminDelay, admin) {
        if (address(usdc_) == address(0) || address(escrow_) == address(0)) revert ZeroAddress();
        if (markupBps_ > MAX_MARKUP_BPS) revert InvalidBps();
        usdc = usdc_;
        escrow = escrow_;
        markupBps = markupBps_;
        // AccessControlDefaultAdminRules already rejects a zero admin.
        // slither-disable-next-line missing-zero-check
        feeRecipient = admin;
        if (registrar != address(0)) _grantRole(REGISTRAR_ROLE, registrar);
        emit MarkupSet(markupBps_);
    }

    // ---------------------------------------------------------------------
    // Trading
    // ---------------------------------------------------------------------

    /// @notice Lists a ticket for sale. The ticket is escrowed in this contract until sold or cancelled.
    /// @dev The seller must have approved this contract for `tokenId`.
    /// @param ticket Ticket contract.
    /// @param tokenId Ticket to sell.
    /// @param price Asking price in USDC; at most {maxResalePrice}.
    /// @return listingId Identifier of the new listing.
    function list(address ticket, uint256 tokenId, uint256 price) external nonReentrant returns (uint256 listingId) {
        _requireResaleOpen(ticket, tokenId);
        if (IEventTicket(ticket).ownerOf(tokenId) != msg.sender) revert NotTicketOwner();
        if (price == 0) revert ZeroPrice();
        uint256 cap = maxResalePrice(ticket, tokenId);
        if (price > cap) revert PriceAboveCap(price, cap);

        listingId = nextListingId++;
        listings[listingId] =
            Listing({seller: msg.sender, ticket: ticket, tokenId: tokenId, price: price, active: true});
        emit Listed(listingId, ticket, tokenId, msg.sender, price);

        IEventTicket(ticket).transferFrom(msg.sender, address(this), tokenId);
    }

    /// @notice Buys a listed ticket. Payment split and ticket delivery happen in one transaction.
    /// @dev The buyer must have approved this contract for the listing price.
    /// @param listingId Listing to buy.
    function buy(uint256 listingId) external nonReentrant {
        Listing storage listing = listings[listingId];
        if (!listing.active) revert ListingNotActive(listingId);
        address ticket = listing.ticket;
        uint256 tokenId = listing.tokenId;
        uint256 price = listing.price;
        address seller = listing.seller;
        _requireResaleOpen(ticket, tokenId);
        // Re-checked at settlement so lowering the markup also applies to listings created before the change.
        uint256 cap = maxResalePrice(ticket, tokenId);
        if (price > cap) revert PriceAboveCap(price, cap);

        (address royaltyReceiver, uint256 royalty) = IERC2981(ticket).royaltyInfo(tokenId, price);
        uint256 fee = (price * protocolFeeBps) / BPS_DENOMINATOR;
        // Royalty is capped at 100% by ERC-2981 and the fee at 10%, so clamp rather than underflow.
        if (royalty + fee > price) royalty = price - fee;
        uint256 sellerProceeds = price - royalty - fee;

        listing.active = false;
        if (royalty != 0) pendingWithdrawals[royaltyReceiver] += royalty;
        if (fee != 0) pendingWithdrawals[feeRecipient] += fee;
        emit Sold(listingId, msg.sender, seller, price, royalty, fee);

        usdc.safeTransferFrom(msg.sender, address(this), price);
        usdc.safeTransfer(seller, sellerProceeds);
        IEventTicket(ticket).transferFrom(address(this), msg.sender, tokenId);
    }

    /// @notice Cancels a listing and returns the ticket to the seller. Always available to the seller.
    /// @param listingId Listing to cancel.
    function cancel(uint256 listingId) external nonReentrant {
        Listing storage listing = listings[listingId];
        if (!listing.active) revert ListingNotActive(listingId);
        if (listing.seller != msg.sender) revert NotSeller();

        listing.active = false;
        emit ListingCancelled(listingId);

        IEventTicket(listing.ticket).transferFrom(address(this), msg.sender, listing.tokenId);
    }

    /// @notice Withdraws accrued royalties or protocol fees.
    function withdraw() external nonReentrant {
        uint256 amount = pendingWithdrawals[msg.sender];
        if (amount == 0) revert NothingToWithdraw();
        pendingWithdrawals[msg.sender] = 0;
        emit Withdrawn(msg.sender, amount);
        usdc.safeTransfer(msg.sender, amount);
    }

    // ---------------------------------------------------------------------
    // Administration
    // ---------------------------------------------------------------------

    /// @notice Registers a ticket contract so its tickets can be traded here.
    /// @param ticket Ticket contract created by the factory.
    function registerTicket(address ticket) external onlyRole(REGISTRAR_ROLE) {
        if (ticket == address(0)) revert ZeroAddress();
        if (isRegisteredTicket[ticket]) revert AlreadyRegistered(ticket);
        isRegisteredTicket[ticket] = true;
        emit TicketRegistered(ticket);
    }

    /// @notice Sets the maximum markup over face value.
    /// @param newMarkupBps Markup in basis points, at most {MAX_MARKUP_BPS}.
    function setMarkupBps(uint16 newMarkupBps) external onlyRole(DEFAULT_ADMIN_ROLE) {
        if (newMarkupBps > MAX_MARKUP_BPS) revert InvalidBps();
        markupBps = newMarkupBps;
        emit MarkupSet(newMarkupBps);
    }

    /// @notice Sets the protocol fee and its recipient.
    /// @param newFeeBps Fee in basis points, at most {MAX_PROTOCOL_FEE_BPS}.
    /// @param newRecipient Account that can withdraw the fees.
    function setProtocolFee(uint16 newFeeBps, address newRecipient) external onlyRole(DEFAULT_ADMIN_ROLE) {
        if (newFeeBps > MAX_PROTOCOL_FEE_BPS) revert InvalidBps();
        if (newRecipient == address(0)) revert ZeroAddress();
        protocolFeeBps = newFeeBps;
        feeRecipient = newRecipient;
        emit ProtocolFeeSet(newFeeBps, newRecipient);
    }

    // ---------------------------------------------------------------------
    // Views
    // ---------------------------------------------------------------------

    /// @notice Highest price a ticket may be listed for: face value plus the capped markup.
    /// @param ticket Ticket contract.
    /// @param tokenId Ticket identifier.
    function maxResalePrice(address ticket, uint256 tokenId) public view returns (uint256) {
        uint256 faceValue = IEventTicket(ticket).tierPrice(IEventTicket(ticket).tierOf(tokenId));
        return faceValue + (faceValue * markupBps) / BPS_DENOMINATOR;
    }

    // ---------------------------------------------------------------------
    // Internals
    // ---------------------------------------------------------------------

    function _requireResaleOpen(address ticket, uint256 tokenId) private view {
        if (!isRegisteredTicket[ticket]) revert NotRegistered(ticket);
        if (block.timestamp >= IEventTicket(ticket).eventEnd() || escrow.isCancelled(ticket)) revert ResaleClosed();
        if (IEventTicket(ticket).isRedeemed(tokenId)) revert TicketRedeemed(tokenId);
    }
}
