// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {IAccessControl} from "@openzeppelin/contracts/access/IAccessControl.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";

import {BaseTest} from "./utils/BaseTest.sol";
import {EventTicket} from "../src/EventTicket.sol";
import {PriceCappedResale} from "../src/PriceCappedResale.sol";
import {IEventEscrow} from "../src/interfaces/IEventEscrow.sol";

contract PriceCappedResaleTest is BaseTest {
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

    uint256 internal constant CAP = GA_PRICE + GA_PRICE / 10;
    uint256 internal tokenId;

    function setUp() public override {
        super.setUp();
        tokenId = _purchase(aliceKey, GA, 1, 1);
    }

    // ----- construction and administration --------------------------------

    function test_Constructor_RevertPaths() public {
        vm.expectRevert(PriceCappedResale.ZeroAddress.selector);
        new PriceCappedResale(IERC20(address(0)), IEventEscrow(address(escrow)), admin, ADMIN_DELAY, 1000, address(0));
        vm.expectRevert(PriceCappedResale.ZeroAddress.selector);
        new PriceCappedResale(IERC20(address(usdc)), IEventEscrow(address(0)), admin, ADMIN_DELAY, 1000, address(0));
        vm.expectRevert(PriceCappedResale.InvalidBps.selector);
        new PriceCappedResale(IERC20(address(usdc)), IEventEscrow(address(escrow)), admin, ADMIN_DELAY, 5001, address(0));
    }

    function test_Defaults() public view {
        assertEq(resale.markupBps(), 1000);
        assertEq(resale.protocolFeeBps(), 0);
        assertEq(resale.feeRecipient(), admin);
        assertTrue(resale.isRegisteredTicket(address(ticket)));
        assertEq(resale.maxResalePrice(address(ticket), tokenId), CAP);
    }

    function test_RegisterTicket_RevertPaths() public {
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, resale.REGISTRAR_ROLE()
            )
        );
        vm.prank(stranger);
        resale.registerTicket(address(1));

        vm.startPrank(address(factory));
        vm.expectRevert(PriceCappedResale.ZeroAddress.selector);
        resale.registerTicket(address(0));
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.AlreadyRegistered.selector, address(ticket)));
        resale.registerTicket(address(ticket));
        vm.stopPrank();
    }

    function test_SetMarkupAndFee() public {
        vm.startPrank(admin);
        resale.setMarkupBps(2000);
        resale.setProtocolFee(250, stranger);
        vm.expectRevert(PriceCappedResale.InvalidBps.selector);
        resale.setMarkupBps(5001);
        vm.expectRevert(PriceCappedResale.InvalidBps.selector);
        resale.setProtocolFee(1001, stranger);
        vm.expectRevert(PriceCappedResale.ZeroAddress.selector);
        resale.setProtocolFee(100, address(0));
        vm.stopPrank();

        assertEq(resale.markupBps(), 2000);
        assertEq(resale.protocolFeeBps(), 250);
        assertEq(resale.feeRecipient(), stranger);
        assertEq(resale.maxResalePrice(address(ticket), tokenId), GA_PRICE + GA_PRICE / 5);
    }

    function test_AdminSetters_RevertWhen_NotAdmin() public {
        bytes memory err = abi.encodeWithSelector(
            IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, resale.DEFAULT_ADMIN_ROLE()
        );
        vm.startPrank(stranger);
        vm.expectRevert(err);
        resale.setMarkupBps(1);
        vm.expectRevert(err);
        resale.setProtocolFee(1, stranger);
        vm.stopPrank();
    }

    // ----- listing --------------------------------------------------------

    function test_List_EscrowsTicket() public {
        vm.startPrank(alice);
        ticket.approve(address(resale), tokenId);
        vm.expectEmit(true, true, true, true, address(resale));
        emit Listed(1, address(ticket), tokenId, alice, CAP);
        uint256 listingId = resale.list(address(ticket), tokenId, CAP);
        vm.stopPrank();

        assertEq(listingId, 1);
        assertEq(ticket.ownerOf(tokenId), address(resale));
        (address seller, address listedTicket, uint256 listedToken, uint256 price, bool active) = resale.listings(1);
        assertEq(seller, alice);
        assertEq(listedTicket, address(ticket));
        assertEq(listedToken, tokenId);
        assertEq(price, CAP);
        assertTrue(active);
    }

    function test_List_RevertWhen_AboveCap() public {
        vm.startPrank(alice);
        ticket.approve(address(resale), tokenId);
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.PriceAboveCap.selector, CAP + 1, CAP));
        resale.list(address(ticket), tokenId, CAP + 1);
        vm.stopPrank();
    }

    function test_List_RevertPaths() public {
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.NotRegistered.selector, stranger));
        resale.list(stranger, tokenId, 1);

        vm.expectRevert(PriceCappedResale.NotTicketOwner.selector);
        vm.prank(bob);
        resale.list(address(ticket), tokenId, GA_PRICE);

        vm.expectRevert(PriceCappedResale.ZeroPrice.selector);
        vm.prank(alice);
        resale.list(address(ticket), tokenId, 0);

        // Without approval the marketplace cannot take the ticket.
        vm.expectRevert();
        vm.prank(alice);
        resale.list(address(ticket), tokenId, GA_PRICE);
    }

    function test_List_RevertWhen_EventEndedOrCancelled() public {
        vm.warp(eventEnd);
        vm.expectRevert(PriceCappedResale.ResaleClosed.selector);
        vm.prank(alice);
        resale.list(address(ticket), tokenId, GA_PRICE);

        vm.warp(START);
        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        vm.expectRevert(PriceCappedResale.ResaleClosed.selector);
        vm.prank(alice);
        resale.list(address(ticket), tokenId, GA_PRICE);
    }

    function test_List_RevertWhen_Redeemed() public {
        _redeemOnChain(ticket, _single(tokenId), 0);
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.TicketRedeemed.selector, tokenId));
        vm.prank(alice);
        resale.list(address(ticket), tokenId, GA_PRICE);
    }

    function test_List_FreeTicketsCannotBeSold() public {
        vm.prank(minter);
        ticket.mintBatch(_order(alice, FREE, 1, 99));
        uint256 freeId = ticket.totalMinted();
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.PriceAboveCap.selector, 1, 0));
        vm.prank(alice);
        resale.list(address(ticket), freeId, 1);
    }

    // ----- buying ---------------------------------------------------------

    function test_Buy_SettlesAtomicallyWithRoyalty() public {
        uint256 listingId = _list(CAP);
        uint256 royalty = (CAP * ROYALTY_BPS) / 10_000;

        usdc.mint(bob, CAP);
        vm.startPrank(bob);
        usdc.approve(address(resale), CAP);
        vm.expectEmit(true, true, true, true, address(resale));
        emit Sold(listingId, bob, alice, CAP, royalty, 0);
        resale.buy(listingId);
        vm.stopPrank();

        assertEq(ticket.ownerOf(tokenId), bob);
        assertEq(usdc.balanceOf(bob), 0);
        assertEq(usdc.balanceOf(alice), CAP - royalty);
        assertEq(resale.pendingWithdrawals(organizer), royalty);
        assertEq(usdc.balanceOf(address(resale)), royalty);

        vm.prank(organizer);
        resale.withdraw();
        assertEq(usdc.balanceOf(organizer), royalty);
        assertEq(usdc.balanceOf(address(resale)), 0);

        vm.expectRevert(PriceCappedResale.NothingToWithdraw.selector);
        vm.prank(organizer);
        resale.withdraw();
    }

    function test_Buy_WithProtocolFee() public {
        vm.prank(admin);
        resale.setProtocolFee(200, stranger);
        uint256 listingId = _list(GA_PRICE);
        uint256 royalty = (GA_PRICE * ROYALTY_BPS) / 10_000;
        uint256 fee = (GA_PRICE * 200) / 10_000;

        _buy(bob, listingId, GA_PRICE);

        assertEq(usdc.balanceOf(alice), GA_PRICE - royalty - fee);
        assertEq(resale.pendingWithdrawals(stranger), fee);
        assertEq(resale.pendingWithdrawals(organizer), royalty);
    }

    function test_Buy_RevertWhen_NotActive() public {
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.ListingNotActive.selector, 1));
        resale.buy(1);

        uint256 listingId = _list(GA_PRICE);
        _buy(bob, listingId, GA_PRICE);
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.ListingNotActive.selector, listingId));
        resale.buy(listingId);
    }

    function test_Buy_RevertWhen_EventEnded() public {
        uint256 listingId = _list(GA_PRICE);
        vm.warp(eventEnd);
        vm.expectRevert(PriceCappedResale.ResaleClosed.selector);
        vm.prank(bob);
        resale.buy(listingId);
    }

    function test_Buy_RevertWhen_MarkupLoweredBelowListingPrice() public {
        uint256 listingId = _list(CAP);
        vm.prank(admin);
        resale.setMarkupBps(0);
        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.PriceAboveCap.selector, CAP, GA_PRICE));
        vm.prank(bob);
        resale.buy(listingId);
    }

    function test_Buy_RevertWhen_BuyerCannotPay() public {
        uint256 listingId = _list(GA_PRICE);
        vm.expectRevert();
        vm.prank(bob);
        resale.buy(listingId);
        assertEq(ticket.ownerOf(tokenId), address(resale));
    }

    // ----- cancelling -----------------------------------------------------

    function test_Cancel_ReturnsTicket() public {
        uint256 listingId = _list(GA_PRICE);

        vm.expectRevert(PriceCappedResale.NotSeller.selector);
        vm.prank(bob);
        resale.cancel(listingId);

        vm.prank(alice);
        resale.cancel(listingId);
        assertEq(ticket.ownerOf(tokenId), alice);

        vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.ListingNotActive.selector, listingId));
        vm.prank(alice);
        resale.cancel(listingId);
    }

    function test_Cancel_WorksAfterEventEnd() public {
        uint256 listingId = _list(GA_PRICE);
        vm.warp(eventEnd + 30 days);
        vm.prank(alice);
        resale.cancel(listingId);
        assertEq(ticket.ownerOf(tokenId), alice);
    }

    // ----- the lock cannot be bypassed around the marketplace --------------

    function test_PrivateSaleAroundTheMarketplaceIsImpossible() public {
        vm.startPrank(alice);
        ticket.approve(bob, tokenId);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.TransferLocked.selector, tokenId, unlockTime));
        ticket.transferFrom(alice, bob, tokenId);
        vm.stopPrank();

        vm.expectRevert(abi.encodeWithSelector(EventTicket.TransferLocked.selector, tokenId, unlockTime));
        vm.prank(bob);
        ticket.transferFrom(alice, bob, tokenId);
    }

    // ----- helpers --------------------------------------------------------

    function _list(uint256 price) private returns (uint256 listingId) {
        vm.startPrank(alice);
        ticket.approve(address(resale), tokenId);
        listingId = resale.list(address(ticket), tokenId, price);
        vm.stopPrank();
    }

    function _buy(address buyer, uint256 listingId, uint256 price) private {
        usdc.mint(buyer, price);
        vm.startPrank(buyer);
        usdc.approve(address(resale), price);
        resale.buy(listingId);
        vm.stopPrank();
    }
}
