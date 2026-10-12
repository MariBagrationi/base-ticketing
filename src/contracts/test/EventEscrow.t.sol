// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {IAccessControl} from "@openzeppelin/contracts/access/IAccessControl.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";

import {BaseTest} from "./utils/BaseTest.sol";
import {MaliciousTicket} from "./mocks/Mocks.sol";
import {EventTicket} from "../src/EventTicket.sol";
import {EventEscrow} from "../src/EventEscrow.sol";
import {IEventEscrow} from "../src/interfaces/IEventEscrow.sol";
import {MockUSDC} from "../src/mocks/MockUSDC.sol";
import {MintVoucher, PaymentAuthorization, PermitSignature} from "../src/TixFlowTypes.sol";

contract EventEscrowTest is BaseTest {
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

    // ----- construction and registration ---------------------------------

    function test_Constructor_RevertWhen_ZeroUsdc() public {
        vm.expectRevert(EventEscrow.ZeroAddress.selector);
        new EventEscrow(IERC20(address(0)), admin, ADMIN_DELAY, address(0), address(0));
    }

    function test_Constructor_WithoutRegistrar() public {
        EventEscrow e = new EventEscrow(IERC20(address(usdc)), admin, ADMIN_DELAY, address(0), address(0));
        assertFalse(e.hasRole(e.REGISTRAR_ROLE(), address(factory)));
        assertFalse(e.hasRole(e.CANCELLER_ROLE(), minter));
    }

    function test_RegisterEvent_OnlyRegistrar() public {
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, escrow.REGISTRAR_ROLE()
            )
        );
        vm.prank(stranger);
        escrow.registerEvent(address(1));
    }

    function test_RegisterEvent_RevertWhen_ZeroOrDuplicate() public {
        vm.startPrank(address(factory));
        vm.expectRevert(EventEscrow.ZeroAddress.selector);
        escrow.registerEvent(address(0));
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.AlreadyRegistered.selector, address(ticket)));
        escrow.registerEvent(address(ticket));
        vm.stopPrank();
    }

    function test_CollectPayment_RevertWhen_CallerNotRegistered() public {
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NotRegistered.selector, stranger));
        vm.prank(stranger);
        escrow.collectPayment(stranger, alice, 1, 1, GA_PRICE);
    }

    // ----- gasless purchase (EIP-3009) ------------------------------------

    function test_PurchaseWithAuthorization_MintsAndEscrowsAtomically() public {
        usdc.mint(alice, GA_PRICE * 2);
        MintVoucher memory v = _voucher(alice, GA, 2, 1);
        bytes memory sig = _signVoucher(v);
        PaymentAuthorization memory auth = _signReceive(aliceKey, address(escrow), GA_PRICE * 2, bytes32("n1"));

        vm.expectEmit(true, true, true, true, address(escrow));
        emit PaymentDeposited(address(ticket), alice, address(escrow), 1, 2, GA_PRICE * 2);
        vm.prank(relayer);
        uint256 first = escrow.purchaseWithAuthorization(address(ticket), v, sig, auth);

        assertEq(first, 1);
        assertEq(ticket.balanceOf(alice), 2);
        assertEq(usdc.balanceOf(alice), 0);
        assertEq(usdc.balanceOf(address(escrow)), GA_PRICE * 2);
        assertEq(escrow.totalHeld(), GA_PRICE * 2);
        assertEq(escrow.paidFor(address(ticket), 1), GA_PRICE);
        assertEq(escrow.paidFor(address(ticket), 2), GA_PRICE);
        (bool registered, bool cancelled, uint256 held, uint256 released) = escrow.eventAccount(address(ticket));
        assertTrue(registered);
        assertFalse(cancelled);
        assertEq(held, GA_PRICE * 2);
        assertEq(released, 0);
    }

    function test_PurchaseWithAuthorization_FreeTierNeedsNoPayment() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        PaymentAuthorization memory auth;
        escrow.purchaseWithAuthorization(address(ticket), v, _signVoucher(v), auth);
        assertEq(ticket.ownerOf(1), alice);
        assertEq(escrow.totalHeld(), 0);
    }

    function test_PurchaseWithAuthorization_RevertWhen_AuthorizationForWrongAmount() public {
        usdc.mint(alice, GA_PRICE * 2);
        MintVoucher memory v = _voucher(alice, GA, 2, 1);
        bytes memory sig = _signVoucher(v);
        PaymentAuthorization memory auth = _signReceive(aliceKey, address(escrow), GA_PRICE, bytes32("n1"));

        vm.expectRevert(MockUSDC.InvalidSignature.selector);
        escrow.purchaseWithAuthorization(address(ticket), v, sig, auth);
        assertEq(ticket.totalMinted(), 0);
    }

    function test_PurchaseWithAuthorization_RevertWhen_AuthorizationReplayed() public {
        usdc.mint(alice, GA_PRICE * 2);
        PaymentAuthorization memory auth = _signReceive(aliceKey, address(escrow), GA_PRICE, bytes32("n1"));
        MintVoucher memory v1 = _voucher(alice, GA, 1, 1);
        escrow.purchaseWithAuthorization(address(ticket), v1, _signVoucher(v1), auth);

        MintVoucher memory v2 = _voucher(alice, GA, 1, 2);
        bytes memory sig2 = _signVoucher(v2);
        vm.expectRevert(MockUSDC.AuthorizationAlreadyUsed.selector);
        escrow.purchaseWithAuthorization(address(ticket), v2, sig2, auth);
    }

    function test_PurchaseWithAuthorization_RevertWhen_VoucherInvalid_AndPaymentIsRolledBack() public {
        usdc.mint(alice, GA_PRICE);
        MintVoucher memory v = _voucher(alice, GA, 1, 1);
        bytes memory badSig = _signVoucher(ticket, v, bobKey);
        PaymentAuthorization memory auth = _signReceive(aliceKey, address(escrow), GA_PRICE, bytes32("n1"));

        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        escrow.purchaseWithAuthorization(address(ticket), v, badSig, auth);
        assertEq(usdc.balanceOf(alice), GA_PRICE);
    }

    function test_Purchase_RevertWhen_TicketNotRegistered() public {
        MintVoucher memory v = _voucher(alice, GA, 1, 1);
        PaymentAuthorization memory auth;
        PermitSignature memory permit;
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NotRegistered.selector, stranger));
        escrow.purchaseWithAuthorization(stranger, v, "", auth);
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NotRegistered.selector, stranger));
        escrow.purchaseWithPermit(stranger, v, "", permit);
    }

    // ----- gasless purchase (EIP-2612) ------------------------------------

    function test_PurchaseWithPermit() public {
        usdc.mint(alice, VIP_PRICE);
        MintVoucher memory v = _voucher(alice, VIP, 1, 1);
        PermitSignature memory permit = _signPermit(aliceKey, address(escrow), VIP_PRICE);
        bytes memory sig = _signVoucher(v);

        vm.prank(relayer);
        escrow.purchaseWithPermit(address(ticket), v, sig, permit);

        assertEq(ticket.ownerOf(1), alice);
        assertEq(usdc.balanceOf(address(escrow)), VIP_PRICE);
        assertEq(escrow.paidFor(address(ticket), 1), VIP_PRICE);
    }

    function test_PurchaseWithPermit_SurvivesFrontRunPermit() public {
        usdc.mint(alice, VIP_PRICE);
        MintVoucher memory v = _voucher(alice, VIP, 1, 1);
        PermitSignature memory permit = _signPermit(aliceKey, address(escrow), VIP_PRICE);

        // An observer submits the permit first; the purchase must still go through.
        vm.prank(stranger);
        usdc.permit(alice, address(escrow), VIP_PRICE, permit.deadline, permit.v, permit.r, permit.s);

        escrow.purchaseWithPermit(address(ticket), v, _signVoucher(v), permit);
        assertEq(ticket.ownerOf(1), alice);
    }

    function test_PurchaseWithPermit_FreeTier() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        PermitSignature memory permit;
        escrow.purchaseWithPermit(address(ticket), v, _signVoucher(v), permit);
        assertEq(ticket.ownerOf(1), alice);
    }

    function test_PurchaseWithPermit_RevertWhen_NoAllowance() public {
        usdc.mint(alice, VIP_PRICE);
        MintVoucher memory v = _voucher(alice, VIP, 1, 1);
        bytes memory sig = _signVoucher(v);
        PermitSignature memory badPermit = _signPermit(bobKey, address(escrow), VIP_PRICE);
        vm.expectRevert();
        escrow.purchaseWithPermit(address(ticket), v, sig, badPermit);
    }

    // ----- prepaid accounting cannot be fooled ----------------------------

    function test_Purchase_RevertWhen_TicketSkipsPaymentAccounting() public {
        MaliciousTicket bad = _registerMalicious(MaliciousTicket.Mode.SkipPayment);
        usdc.mint(alice, 10e6);
        MintVoucher memory v = _voucher(alice, 1, 1, 1);
        PaymentAuthorization memory auth = _signReceive(aliceKey, address(escrow), 10e6, bytes32("n1"));

        vm.expectRevert(EventEscrow.PrepaidMismatch.selector);
        escrow.purchaseWithAuthorization(address(bad), v, "", auth);
        assertEq(usdc.balanceOf(alice), 10e6);
    }

    function test_Purchase_RevertWhen_TicketAccountsWrongAmount() public {
        MaliciousTicket bad = _registerMalicious(MaliciousTicket.Mode.WrongAmount);
        usdc.mint(alice, 10e6);
        MintVoucher memory v = _voucher(alice, 1, 1, 1);
        PermitSignature memory permit = _signPermit(aliceKey, address(escrow), 10e6);

        vm.expectRevert(EventEscrow.PrepaidMismatch.selector);
        escrow.purchaseWithPermit(address(bad), v, "", permit);
    }

    // ----- release --------------------------------------------------------

    function test_Release_PaysOrganizerAfterEventEnd() public {
        _purchase(aliceKey, GA, 2, 1);
        _purchase(bobKey, VIP, 1, 2);
        uint256 total = GA_PRICE * 2 + VIP_PRICE;

        vm.expectRevert(abi.encodeWithSelector(EventEscrow.EventNotEnded.selector, address(ticket), eventEnd));
        escrow.release(address(ticket));

        vm.warp(eventEnd);
        vm.expectEmit(true, true, false, true, address(escrow));
        emit EscrowReleased(address(ticket), organizer, total);
        vm.prank(stranger);
        escrow.release(address(ticket));

        assertEq(usdc.balanceOf(organizer), total);
        assertEq(usdc.balanceOf(address(escrow)), 0);
        assertEq(escrow.totalHeld(), 0);
        (,, uint256 held, uint256 released) = escrow.eventAccount(address(ticket));
        assertEq(held, 0);
        assertEq(released, total);

        vm.expectRevert(EventEscrow.NothingToRelease.selector);
        escrow.release(address(ticket));
    }

    function test_Release_RevertWhen_NotRegisteredOrCancelled() public {
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NotRegistered.selector, stranger));
        escrow.release(stranger);

        _purchase(aliceKey, GA, 1, 1);
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        vm.warp(eventEnd);
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.EventIsCancelled.selector, address(ticket)));
        escrow.release(address(ticket));
    }

    // ----- cancellation and refunds ---------------------------------------

    function test_CancelEvent_ByCancellerRole() public {
        assertTrue(escrow.hasRole(escrow.CANCELLER_ROLE(), minter));
        vm.prank(minter);
        escrow.cancelEvent(address(ticket));
        assertTrue(escrow.isCancelled(address(ticket)));
    }

    function test_CancelEvent_ByOrganizerOrAdmin() public {
        vm.expectEmit(true, true, false, false, address(escrow));
        emit EventCancelled(address(ticket), organizer);
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        assertTrue(escrow.isCancelled(address(ticket)));

        EventTicket other = _createEvent(keccak256("event-2"), "Other");
        vm.prank(admin);
        escrow.cancelEvent(address(other));
        assertTrue(escrow.isCancelled(address(other)));
    }

    function test_CancelEvent_RevertPaths() public {
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NotRegistered.selector, stranger));
        escrow.cancelEvent(stranger);

        vm.expectRevert(EventEscrow.NotAuthorized.selector);
        vm.prank(stranger);
        escrow.cancelEvent(address(ticket));

        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.EventIsCancelled.selector, address(ticket)));
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
    }

    function test_CancelEvent_RevertWhen_FundsAlreadyReleased() public {
        _purchase(aliceKey, GA, 1, 1);
        vm.warp(eventEnd);
        escrow.release(address(ticket));
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.AlreadyReleased.selector, address(ticket)));
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
    }

    function test_Purchase_RevertWhen_EventCancelled() public {
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));

        usdc.mint(alice, GA_PRICE);
        MintVoucher memory v = _voucher(alice, GA, 1, 1);
        bytes memory sig = _signVoucher(v);
        PaymentAuthorization memory auth = _signReceive(aliceKey, address(escrow), GA_PRICE, bytes32("n1"));
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.EventIsCancelled.selector, address(ticket)));
        escrow.purchaseWithAuthorization(address(ticket), v, sig, auth);
    }

    function test_ClaimRefund_PaysFullPriceToHolder() public {
        uint256 id = _purchase(aliceKey, VIP, 1, 1);

        vm.expectRevert(abi.encodeWithSelector(EventEscrow.EventNotCancelled.selector, address(ticket)));
        escrow.claimRefund(address(ticket), id);

        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));

        vm.expectEmit(true, true, true, true, address(escrow));
        emit Refunded(address(ticket), id, alice, VIP_PRICE);
        vm.prank(stranger);
        escrow.claimRefund(address(ticket), id);

        assertEq(usdc.balanceOf(alice), VIP_PRICE);
        assertEq(escrow.totalHeld(), 0);
        assertEq(escrow.paidFor(address(ticket), id), 0);

        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NothingToRefund.selector, id));
        escrow.claimRefund(address(ticket), id);
    }

    function test_ClaimRefund_GoesToCurrentHolderAfterResale() public {
        uint256 id = _purchase(aliceKey, GA, 1, 1);
        vm.startPrank(alice);
        ticket.approve(address(resale), id);
        uint256 listingId = resale.list(address(ticket), id, GA_PRICE);
        vm.stopPrank();

        usdc.mint(bob, GA_PRICE);
        vm.startPrank(bob);
        usdc.approve(address(resale), GA_PRICE);
        resale.buy(listingId);
        vm.stopPrank();

        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        escrow.claimRefund(address(ticket), id);
        assertEq(usdc.balanceOf(bob), GA_PRICE);
    }

    function test_ClaimRefund_RevertWhen_TicketWasNotPaidOnChain() public {
        vm.prank(minter);
        ticket.mintBatch(_order(alice, GA, 1, 1));
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.NothingToRefund.selector, 1));
        escrow.claimRefund(address(ticket), 1);
    }

    function test_Refund_ListedTicket_StrictRevertsAndBatchSkips() public {
        uint256 listed = _purchase(aliceKey, GA, 1, 1);
        uint256 held = _purchase(bobKey, GA, 1, 2);
        vm.startPrank(alice);
        ticket.approve(address(resale), listed);
        uint256 listingId = resale.list(address(ticket), listed, GA_PRICE);
        vm.stopPrank();

        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));

        vm.expectRevert(abi.encodeWithSelector(EventEscrow.TicketListed.selector, listed));
        escrow.claimRefund(address(ticket), listed);

        uint256[] memory ids = new uint256[](3);
        (ids[0], ids[1], ids[2]) = (listed, held, held);
        escrow.refundBatch(address(ticket), ids);
        assertEq(usdc.balanceOf(bob), GA_PRICE);
        assertEq(usdc.balanceOf(alice), 0);

        // Once the seller takes the ticket back, the refund is available again.
        vm.prank(alice);
        resale.cancel(listingId);
        escrow.claimRefund(address(ticket), listed);
        assertEq(usdc.balanceOf(alice), GA_PRICE);
        assertEq(escrow.totalHeld(), 0);
    }

    function test_RefundBatch_RevertWhen_NotCancelled() public {
        vm.expectRevert(abi.encodeWithSelector(EventEscrow.EventNotCancelled.selector, address(ticket)));
        escrow.refundBatch(address(ticket), new uint256[](0));
    }

    // ----- helpers --------------------------------------------------------

    function _registerMalicious(MaliciousTicket.Mode mode) private returns (MaliciousTicket bad) {
        bad = new MaliciousTicket(IEventEscrow(address(escrow)));
        bad.setMode(mode);
        vm.prank(address(factory));
        escrow.registerEvent(address(bad));
    }
}
