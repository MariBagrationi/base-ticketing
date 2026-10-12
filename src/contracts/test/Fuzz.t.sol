// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {BaseTest} from "./utils/BaseTest.sol";
import {EventTicket} from "../src/EventTicket.sol";
import {PriceCappedResale} from "../src/PriceCappedResale.sol";
import {TicketFactory} from "../src/TicketFactory.sol";
import {MintVoucher, TierInit, PaymentAuthorization} from "../src/TixFlowTypes.sol";

contract FuzzTest is BaseTest {
    /// @notice No sequence of voucher mints can push a wallet past the tier's per-wallet limit.
    function testFuzz_MaxPerWalletNeverExceeded(uint8[12] calldata quantities) public {
        (,,,, uint256 maxPerWallet) = ticket.tierConfig(GA);

        for (uint256 i = 0; i < quantities.length; ++i) {
            uint256 quantity = bound(quantities[i], 0, 6);
            uint256 cost = GA_PRICE * quantity;
            usdc.mint(alice, cost);
            vm.prank(alice);
            usdc.approve(address(escrow), cost);

            MintVoucher memory v = _voucher(alice, GA, quantity, i);
            bytes memory sig = _signVoucher(v);
            vm.prank(alice);
            try ticket.mintWithVoucher(v, sig) {} catch {}

            assertLe(ticket.mintedBy(GA, alice), maxPerWallet);
            assertLe(ticket.balanceOf(alice), maxPerWallet);
        }
    }

    /// @notice The minter role is bound by the same per-wallet and supply limits as everyone else.
    function testFuzz_MintBatchRespectsLimits(uint8[12] calldata quantities, uint8[12] calldata tiers) public {
        for (uint256 i = 0; i < quantities.length; ++i) {
            uint256 tierId = bound(tiers[i], GA, FREE);
            vm.prank(minter);
            ticket.mintBatch(_order(alice, tierId, quantities[i], i));

            (,, uint256 cap, uint256 minted, uint256 maxPerWallet) = ticket.tierConfig(tierId);
            assertLe(minted, cap);
            if (maxPerWallet != 0) assertLe(ticket.mintedBy(tierId, alice), maxPerWallet);
        }
        assertEq(ticket.balanceOf(alice), ticket.totalMinted());
    }

    /// @notice A voucher can be used exactly once, whatever its contents.
    function testFuzz_VoucherReplayAlwaysFails(uint256 quantity, uint256 nonce, uint256 lifetime) public {
        quantity = bound(quantity, 1, 50);
        lifetime = bound(lifetime, 0, 365 days);

        MintVoucher memory v = MintVoucher({
            buyer: alice,
            tierId: FREE,
            quantity: quantity,
            nonce: nonce,
            deadline: block.timestamp + lifetime
        });
        bytes memory sig = _signVoucher(v);
        ticket.mintWithVoucher(v, sig);

        vm.expectRevert(abi.encodeWithSelector(EventTicket.VoucherAlreadyUsed.selector, alice, nonce));
        ticket.mintWithVoucher(v, sig);

        vm.expectRevert(abi.encodeWithSelector(EventTicket.VoucherAlreadyUsed.selector, alice, nonce));
        vm.prank(relayer);
        ticket.mintWithVoucher(v, sig);
        assertEq(ticket.balanceOf(alice), quantity);
    }

    /// @notice Changing any signed field invalidates the signature.
    function testFuzz_VoucherTamperingFails(uint8 field, uint256 delta) public {
        delta = bound(delta, 1, type(uint64).max);
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(v);

        field = uint8(bound(field, 0, 4));
        if (field == 0) v.buyer = address(uint160(uint256(uint160(v.buyer)) + delta));
        else if (field == 1) v.tierId += delta;
        else if (field == 2) v.quantity += delta;
        else if (field == 3) v.nonce += delta;
        else v.deadline += delta;

        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        ticket.mintWithVoucher(v, sig);
    }

    /// @notice A signature from any key other than the configured signer is rejected.
    function testFuzz_VoucherFromWrongKeyFails(uint256 key) public {
        key = bound(key, 1, 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364140);
        vm.assume(key != signerKey);
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(ticket, v, key);
        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        ticket.mintWithVoucher(v, sig);
    }

    /// @notice An expired voucher never mints.
    function testFuzz_ExpiredVoucherFails(uint256 lateBy) public {
        lateBy = bound(lateBy, 1, 10 * 365 days);
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(v);
        vm.warp(v.deadline + lateBy);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.VoucherExpired.selector, v.deadline));
        ticket.mintWithVoucher(v, sig);
    }

    /// @notice For any face value and markup, a listing succeeds exactly when it is at or below the cap, and
    ///         the buyer never pays more than the cap.
    function testFuzz_ResalePriceCapNeverExceeded(uint256 faceValue, uint16 markupBps, uint256 askPrice) public {
        faceValue = bound(faceValue, 1, 100_000e6);
        markupBps = uint16(bound(markupBps, 0, resale.MAX_MARKUP_BPS()));
        uint256 cap = faceValue + (faceValue * markupBps) / 10_000;
        askPrice = bound(askPrice, 1, cap * 3 + 10);

        vm.startPrank(admin);
        resale.setMarkupBps(markupBps);
        ticket.configureTier(GA, "General Admission", faceValue, 100, 4);
        vm.stopPrank();

        vm.prank(minter);
        ticket.mintBatch(_order(alice, GA, 1, 1));

        vm.startPrank(alice);
        ticket.approve(address(resale), 1);
        if (askPrice > cap) {
            vm.expectRevert(abi.encodeWithSelector(PriceCappedResale.PriceAboveCap.selector, askPrice, cap));
            resale.list(address(ticket), 1, askPrice);
            vm.stopPrank();
            return;
        }
        uint256 listingId = resale.list(address(ticket), 1, askPrice);
        vm.stopPrank();

        deal(address(usdc), bob, cap * 3 + 10);
        uint256 bobBefore = usdc.balanceOf(bob);
        vm.startPrank(bob);
        usdc.approve(address(resale), type(uint256).max);
        resale.buy(listingId);
        vm.stopPrank();

        uint256 paid = bobBefore - usdc.balanceOf(bob);
        assertEq(paid, askPrice);
        assertLe(paid, cap);
        // Every unit the buyer paid is accounted for: seller proceeds plus accrued royalty.
        assertEq(usdc.balanceOf(alice) + resale.pendingWithdrawals(organizer), askPrice);
    }

    /// @notice Refunds return exactly what was paid, for any price and quantity.
    function testFuzz_RefundEqualsPayment(uint256 price, uint256 quantity) public {
        price = bound(price, 1, 2_000e6);
        quantity = bound(quantity, 1, 4);
        vm.prank(admin);
        ticket.configureTier(GA, "General Admission", price, 100, 4);

        uint256 first = _purchase(aliceKey, GA, quantity, 1);
        assertEq(escrow.totalHeld(), price * quantity);

        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        for (uint256 i = 0; i < quantity; ++i) {
            escrow.claimRefund(address(ticket), first + i);
        }

        assertEq(usdc.balanceOf(alice), price * quantity);
        assertEq(usdc.balanceOf(address(escrow)), 0);
        assertEq(escrow.totalHeld(), 0);
    }

    /// @notice A tier never mints past its supply cap, for any cap and any stream of buyers.
    function testFuzz_SupplyCapNeverExceeded(uint8 cap, uint8[16] calldata quantities) public {
        uint256 supplyCap = bound(cap, 1, 40);
        vm.prank(admin);
        ticket.configureTier(7, "Fuzz", 0, supplyCap, 0);

        for (uint256 i = 0; i < quantities.length; ++i) {
            address buyer = address(uint160(0x1000 + i));
            MintVoucher memory v = _voucher(buyer, 7, bound(quantities[i], 0, 12), i);
            bytes memory sig = _signVoucher(v);
            try ticket.mintWithVoucher(v, sig) {} catch {}

            (,,, uint256 minted,) = ticket.tierConfig(7);
            assertLe(minted, supplyCap);
        }
    }
}
