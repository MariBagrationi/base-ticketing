// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {AccessControlDefaultAdminRules} from
    "@openzeppelin/contracts/access/extensions/AccessControlDefaultAdminRules.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import {IERC20Permit} from "@openzeppelin/contracts/token/ERC20/extensions/IERC20Permit.sol";
import {SafeERC20} from "@openzeppelin/contracts/token/ERC20/utils/SafeERC20.sol";

import {IEventEscrow} from "./interfaces/IEventEscrow.sol";
import {IEventTicket} from "./interfaces/IEventTicket.sol";
import {IERC3009} from "./interfaces/IERC3009.sol";
import {MintVoucher, PaymentAuthorization, PermitSignature} from "./TixFlowTypes.sol";

/// @title EventEscrow
/// @notice Holds primary-sale USDC for every event until the event has ended.
/// @dev Buyers do not trust the platform with their money: funds reach the organizer only after `eventEnd`,
///      and if the event is cancelled every ticket holder can pull a full refund without anyone's permission.
///      Accounting invariant: `totalHeld` equals the sum of `held` over all events and never exceeds the
///      contract's USDC balance.
contract EventEscrow is IEventEscrow, AccessControlDefaultAdminRules, ReentrancyGuard {
    using SafeERC20 for IERC20;

    /// @notice May register ticket contracts (the factory).
    bytes32 public constant REGISTRAR_ROLE = keccak256("REGISTRAR_ROLE");
    /// @notice May cancel events on the organizer's or platform's instruction (the backend relayer).
    /// @dev Cancelling only makes buyers refundable; it can never move funds to the caller.
    bytes32 public constant CANCELLER_ROLE = keccak256("CANCELLER_ROLE");

    struct EventAccount {
        bool registered;
        bool cancelled;
        uint256 held;
        uint256 released;
    }

    /// @notice Payment token (USDC).
    IERC20 public immutable usdc;

    /// @notice USDC currently held across all events.
    uint256 public totalHeld;

    mapping(address ticket => EventAccount) private _events;
    /// @notice Refundable amount recorded for each ticket at primary sale.
    mapping(address ticket => mapping(uint256 tokenId => uint256)) public paidFor;

    /// @dev Amount pulled by a `purchaseWith*` call that the nested `collectPayment` must account for.
    uint256 private _prepaid;

    event EventRegistered(address indexed ticket);
    event PaymentDeposited(
        address indexed ticket,
        address indexed buyer,
        address indexed payer,
        uint256 firstTokenId,
        uint256 quantity,
        uint256 amount
    );
    event EscrowReleased(address indexed ticket, address indexed organizer, uint256 amount);
    event EventCancelled(address indexed ticket, address indexed cancelledBy);
    event Refunded(address indexed ticket, uint256 indexed tokenId, address indexed holder, uint256 amount);

    error ZeroAddress();
    error AlreadyRegistered(address ticket);
    error NotRegistered(address ticket);
    error EventIsCancelled(address ticket);
    error EventNotCancelled(address ticket);
    error EventNotEnded(address ticket, uint64 eventEnd);
    error AlreadyReleased(address ticket);
    error NotAuthorized();
    error NothingToRelease();
    error NothingToRefund(uint256 tokenId);
    error TicketListed(uint256 tokenId);
    error PrepaidMismatch();

    /// @param usdc_ Payment token.
    /// @param admin Default admin (two-step, delayed transfer).
    /// @param adminDelay Delay applied to admin transfers, in seconds.
    /// @param registrar Initial holder of {REGISTRAR_ROLE} (the factory); zero to grant it later.
    /// @param canceller Initial holder of {CANCELLER_ROLE} (the backend relayer); zero to grant it later.
    constructor(IERC20 usdc_, address admin, uint48 adminDelay, address registrar, address canceller)
        AccessControlDefaultAdminRules(adminDelay, admin)
    {
        if (address(usdc_) == address(0)) revert ZeroAddress();
        usdc = usdc_;
        if (registrar != address(0)) _grantRole(REGISTRAR_ROLE, registrar);
        if (canceller != address(0)) _grantRole(CANCELLER_ROLE, canceller);
    }

    // ---------------------------------------------------------------------
    // Registration and payment
    // ---------------------------------------------------------------------

    /// @notice Registers a ticket contract so it may deposit payments.
    /// @param ticket Ticket contract created by the factory.
    function registerEvent(address ticket) external onlyRole(REGISTRAR_ROLE) {
        if (ticket == address(0)) revert ZeroAddress();
        EventAccount storage account = _events[ticket];
        if (account.registered) revert AlreadyRegistered(ticket);
        account.registered = true;
        emit EventRegistered(ticket);
    }

    /// @notice Records payment for freshly minted tickets and pulls the USDC from the payer.
    /// @dev Only callable by a registered ticket contract, from inside its `mintWithVoucher`.
    /// @param payer Account the USDC is pulled from; this contract itself when the purchase was prepaid.
    /// @param buyer Account that received the tickets.
    /// @param firstTokenId First minted token ID.
    /// @param quantity Number of consecutive tickets minted.
    /// @param unitPrice Price of one ticket.
    function collectPayment(address payer, address buyer, uint256 firstTokenId, uint256 quantity, uint256 unitPrice)
        external
    {
        EventAccount storage account = _events[msg.sender];
        if (!account.registered) revert NotRegistered(msg.sender);
        if (account.cancelled) revert EventIsCancelled(msg.sender);

        uint256 amount = unitPrice * quantity;
        for (uint256 i = 0; i < quantity; ++i) {
            paidFor[msg.sender][firstTokenId + i] = unitPrice;
        }
        account.held += amount;
        totalHeld += amount;
        emit PaymentDeposited(msg.sender, buyer, payer, firstTokenId, quantity, amount);

        if (payer == address(this)) {
            if (_prepaid != amount) revert PrepaidMismatch();
            _prepaid = 0;
        } else {
            // `payer` is whoever called the registered ticket's `mintWithVoucher`, never a caller-chosen account.
            // slither-disable-next-line arbitrary-send-erc20
            usdc.safeTransferFrom(payer, address(this), amount);
        }
    }

    /// @notice Buys tickets with a gasless EIP-3009 USDC authorisation signed by the buyer.
    /// @dev Anyone may relay it: the voucher fixes what is minted and to whom, and the authorisation can only
    ///      move the buyer's USDC into this contract. Payment and mint are atomic.
    /// @param ticket Ticket contract of the event.
    /// @param voucher Backend-signed mint voucher.
    /// @param voucherSignature Signature over the voucher.
    /// @param auth Buyer's `receiveWithAuthorization` signature for exactly the order total.
    /// @return firstTokenId First minted token ID.
    function purchaseWithAuthorization(
        address ticket,
        MintVoucher calldata voucher,
        bytes calldata voucherSignature,
        PaymentAuthorization calldata auth
    ) external nonReentrant returns (uint256 firstTokenId) {
        uint256 amount = _quote(ticket, voucher);
        if (amount != 0) {
            _prepaid = amount;
            IERC3009(address(usdc)).receiveWithAuthorization(
                voucher.buyer, address(this), amount, auth.validAfter, auth.validBefore, auth.nonce, auth.v, auth.r, auth.s
            );
        }
        firstTokenId = IEventTicket(ticket).mintWithVoucher(voucher, voucherSignature);
        if (_prepaid != 0) revert PrepaidMismatch();
    }

    /// @notice Buys tickets with an EIP-2612 permit signed by the buyer.
    /// @dev The permit call is allowed to fail (for example if it was front-run) as long as the resulting
    ///      allowance covers the order.
    /// @param ticket Ticket contract of the event.
    /// @param voucher Backend-signed mint voucher.
    /// @param voucherSignature Signature over the voucher.
    /// @param permit Buyer's permit for at least the order total, with this contract as spender.
    /// @return firstTokenId First minted token ID.
    function purchaseWithPermit(
        address ticket,
        MintVoucher calldata voucher,
        bytes calldata voucherSignature,
        PermitSignature calldata permit
    ) external nonReentrant returns (uint256 firstTokenId) {
        uint256 amount = _quote(ticket, voucher);
        if (amount != 0) {
            _prepaid = amount;
            try IERC20Permit(address(usdc)).permit(
                voucher.buyer, address(this), amount, permit.deadline, permit.v, permit.r, permit.s
            ) {} catch {}
            // The pull is bounded by the buyer's own permit and only happens for a backend-signed voucher
            // naming that buyer, in exchange for tickets minted to them in this same transaction.
            // slither-disable-next-line arbitrary-send-erc20-permit
            usdc.safeTransferFrom(voucher.buyer, address(this), amount);
        }
        firstTokenId = IEventTicket(ticket).mintWithVoucher(voucher, voucherSignature);
        if (_prepaid != 0) revert PrepaidMismatch();
    }

    // ---------------------------------------------------------------------
    // Settlement
    // ---------------------------------------------------------------------

    /// @notice Pays everything held for an event to its organizer once the event has ended.
    /// @dev Permissionless; the destination is fixed by the ticket contract.
    /// @param ticket Ticket contract of the event.
    function release(address ticket) external nonReentrant {
        EventAccount storage account = _events[ticket];
        if (!account.registered) revert NotRegistered(ticket);
        if (account.cancelled) revert EventIsCancelled(ticket);
        uint64 eventEnd = IEventTicket(ticket).eventEnd();
        if (block.timestamp < eventEnd) revert EventNotEnded(ticket, eventEnd);

        uint256 amount = account.held;
        if (amount == 0) revert NothingToRelease();
        address organizer = IEventTicket(ticket).organizer();

        account.held = 0;
        account.released += amount;
        totalHeld -= amount;
        emit EscrowReleased(ticket, organizer, amount);

        usdc.safeTransfer(organizer, amount);
    }

    /// @notice Cancels an event, making every ticket refundable. Irreversible.
    /// @dev Callable by the organizer, the admin or a {CANCELLER_ROLE} holder, and only before any funds were
    ///      released.
    /// @param ticket Ticket contract of the event.
    function cancelEvent(address ticket) external {
        EventAccount storage account = _events[ticket];
        if (!account.registered) revert NotRegistered(ticket);
        if (
            msg.sender != IEventTicket(ticket).organizer() && !hasRole(DEFAULT_ADMIN_ROLE, msg.sender)
                && !hasRole(CANCELLER_ROLE, msg.sender)
        ) revert NotAuthorized();
        if (account.cancelled) revert EventIsCancelled(ticket);
        if (account.released != 0) revert AlreadyReleased(ticket);

        account.cancelled = true;
        emit EventCancelled(ticket, msg.sender);
    }

    /// @notice Refunds the primary-sale price of a ticket of a cancelled event to its current holder.
    /// @dev Permissionless, so holders never depend on the platform and a relayer can refund on their behalf.
    /// @param ticket Ticket contract of the cancelled event.
    /// @param tokenId Ticket to refund.
    function claimRefund(address ticket, uint256 tokenId) external nonReentrant {
        if (!_events[ticket].cancelled) revert EventNotCancelled(ticket);
        if (_refund(ticket, tokenId, true) == 0) revert NothingToRefund(tokenId);
    }

    /// @notice Refunds many tickets of a cancelled event, skipping any that are already refunded, were never
    ///         paid for on-chain, or are currently listed on the marketplace.
    /// @param ticket Ticket contract of the cancelled event.
    /// @param tokenIds Tickets to refund.
    function refundBatch(address ticket, uint256[] calldata tokenIds) external nonReentrant {
        if (!_events[ticket].cancelled) revert EventNotCancelled(ticket);
        uint256 length = tokenIds.length;
        for (uint256 i = 0; i < length; ++i) {
            _refund(ticket, tokenIds[i], false);
        }
    }

    // ---------------------------------------------------------------------
    // Views
    // ---------------------------------------------------------------------

    /// @inheritdoc IEventEscrow
    function isCancelled(address ticket) external view returns (bool) {
        return _events[ticket].cancelled;
    }

    /// @notice Escrow state of an event.
    /// @param ticket Ticket contract of the event.
    function eventAccount(address ticket)
        external
        view
        returns (bool registered, bool cancelled, uint256 held, uint256 released)
    {
        EventAccount storage account = _events[ticket];
        return (account.registered, account.cancelled, account.held, account.released);
    }

    // ---------------------------------------------------------------------
    // Internals
    // ---------------------------------------------------------------------

    function _quote(address ticket, MintVoucher calldata voucher) private view returns (uint256) {
        if (!_events[ticket].registered) revert NotRegistered(ticket);
        return IEventTicket(ticket).tierPrice(voucher.tierId) * voucher.quantity;
    }

    function _refund(address ticket, uint256 tokenId, bool strict) private returns (uint256 amount) {
        amount = paidFor[ticket][tokenId];
        if (amount == 0) return 0;

        address holder = IEventTicket(ticket).ownerOf(tokenId);
        // A listed ticket is held by the marketplace; the seller must cancel the listing first.
        if (holder == IEventTicket(ticket).resaleContract()) {
            if (strict) revert TicketListed(tokenId);
            return 0;
        }

        paidFor[ticket][tokenId] = 0;
        _events[ticket].held -= amount;
        totalHeld -= amount;
        emit Refunded(ticket, tokenId, holder, amount);

        usdc.safeTransfer(holder, amount);
    }
}
