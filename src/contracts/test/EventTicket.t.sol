// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Clones} from "@openzeppelin/contracts/proxy/Clones.sol";
import {IAccessControl} from "@openzeppelin/contracts/access/IAccessControl.sol";
import {IERC721} from "@openzeppelin/contracts/token/ERC721/IERC721.sol";
import {IERC2981} from "@openzeppelin/contracts/interfaces/IERC2981.sol";
import {IERC721Errors} from "@openzeppelin/contracts/interfaces/draft-IERC6093.sol";
import {Initializable} from "@openzeppelin/contracts-upgradeable/proxy/utils/Initializable.sol";
import {PausableUpgradeable} from "@openzeppelin/contracts-upgradeable/utils/PausableUpgradeable.sol";

import {BaseTest} from "./utils/BaseTest.sol";
import {TextUtils} from "./utils/TextUtils.sol";
import {Mock1271Wallet} from "./mocks/Mocks.sol";
import {EventTicket} from "../src/EventTicket.sol";
import {MintVoucher, MintOrder, TierInit, TicketInit} from "../src/TixFlowTypes.sol";

contract EventTicketTest is BaseTest {
    event Minted(
        address indexed buyer, uint256 indexed tierId, uint256 indexed nonce, uint256 firstTokenId, uint256 quantity
    );
    event OrderRejected(address indexed buyer, uint256 indexed nonce, EventTicket.MintRejection reason);
    event Redeemed(uint256 indexed tokenId, address indexed holder);

    // ----- initialisation -------------------------------------------------

    function test_Initialize_SetsState() public view {
        assertEq(ticket.name(), "Neon Arches");
        assertEq(ticket.symbol(), "TIX");
        assertEq(ticket.organizer(), organizer);
        assertEq(ticket.escrow(), address(escrow));
        assertEq(ticket.resaleContract(), address(resale));
        assertEq(ticket.renderer(), address(renderer));
        assertEq(ticket.voucherSigner(), voucherSigner);
        assertEq(ticket.eventStart(), eventStart);
        assertEq(ticket.eventEnd(), eventEnd);
        assertEq(ticket.unlockTime(), unlockTime);
        assertEq(ticket.defaultAdmin(), admin);
        assertEq(ticket.defaultAdminDelay(), ADMIN_DELAY);
        assertTrue(ticket.hasRole(ticket.MINTER_ROLE(), minter));
        assertTrue(ticket.hasRole(ticket.REDEEMER_ROLE(), redeemer));
        assertTrue(ticket.hasRole(ticket.PAUSER_ROLE(), pauser));
        assertEq(ticket.totalMinted(), 0);

        (string memory name, uint256 price, uint256 cap, uint256 minted, uint256 maxPerWallet) = ticket.tierConfig(VIP);
        assertEq(name, "VIP");
        assertEq(price, VIP_PRICE);
        assertEq(cap, 10);
        assertEq(minted, 0);
        assertEq(maxPerWallet, 2);
    }

    function test_Initialize_RevertWhen_CalledTwice() public {
        vm.expectRevert(Initializable.InvalidInitialization.selector);
        ticket.initialize(_init());
    }

    function test_Initialize_RevertWhen_CalledOnImplementation() public {
        vm.expectRevert(Initializable.InvalidInitialization.selector);
        implementation.initialize(_init());
    }

    function test_Initialize_RevertWhen_ZeroOrganizer() public {
        TicketInit memory p = _init();
        p.organizer = address(0);
        _expectInitRevert(p, EventTicket.ZeroAddress.selector);
    }

    function test_Initialize_RevertWhen_ZeroEscrow() public {
        TicketInit memory p = _init();
        p.escrow = address(0);
        _expectInitRevert(p, EventTicket.ZeroAddress.selector);
    }

    function test_Initialize_RevertWhen_ZeroRenderer() public {
        TicketInit memory p = _init();
        p.renderer = address(0);
        _expectInitRevert(p, EventTicket.ZeroAddress.selector);
    }

    function test_Initialize_RevertWhen_EndNotAfterStart() public {
        TicketInit memory p = _init();
        p.eventEnd = p.eventStart;
        _expectInitRevert(p, EventTicket.InvalidSchedule.selector);
    }

    function test_Initialize_AllowsMissingOptionalRoles() public {
        TicketInit memory p = _init();
        p.minter = address(0);
        p.redeemer = address(0);
        p.pauser = address(0);
        EventTicket t = EventTicket(Clones.clone(address(implementation)));
        t.initialize(p);
        assertFalse(t.hasRole(t.MINTER_ROLE(), minter));
    }

    // ----- mintBatch ------------------------------------------------------

    function test_MintBatch_MintsAndEmits() public {
        vm.expectEmit(true, true, true, true, address(ticket));
        emit Minted(alice, GA, 7, 1, 3);
        vm.prank(minter);
        ticket.mintBatch(_order(alice, GA, 3, 7));

        assertEq(ticket.balanceOf(alice), 3);
        assertEq(ticket.ownerOf(1), alice);
        assertEq(ticket.ownerOf(3), alice);
        assertEq(ticket.tierOf(2), GA);
        assertEq(ticket.totalMinted(), 3);
        assertEq(ticket.mintedBy(GA, alice), 3);
        assertTrue(ticket.nonceUsed(alice, 7));
    }

    function test_MintBatch_MultipleOrders() public {
        MintOrder[] memory orders = new MintOrder[](2);
        orders[0] = MintOrder(alice, GA, 2, 1);
        orders[1] = MintOrder(bob, VIP, 1, 1);
        vm.prank(minter);
        ticket.mintBatch(orders);

        assertEq(ticket.ownerOf(2), alice);
        assertEq(ticket.ownerOf(3), bob);
        assertEq(ticket.tierOf(3), VIP);
    }

    function test_MintBatch_RevertWhen_NotMinter() public {
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, ticket.MINTER_ROLE()
            )
        );
        vm.prank(stranger);
        ticket.mintBatch(_order(alice, GA, 1, 1));
    }

    function test_MintBatch_SkipsUsedNonceWithoutReverting() public {
        MintOrder[] memory orders = new MintOrder[](2);
        orders[0] = MintOrder(alice, GA, 1, 1);
        orders[1] = MintOrder(alice, GA, 1, 1);

        vm.expectEmit(true, true, false, true, address(ticket));
        emit OrderRejected(alice, 1, EventTicket.MintRejection.NonceUsed);
        vm.prank(minter);
        ticket.mintBatch(orders);

        assertEq(ticket.balanceOf(alice), 1);
    }

    function test_MintBatch_RejectsInvalidOrdersWithoutReverting() public {
        _expectRejected(_order(address(0), GA, 1, 1), EventTicket.MintRejection.ZeroAddress);
        _expectRejected(_order(alice, GA, 0, 2), EventTicket.MintRejection.ZeroQuantity);
        _expectRejected(_order(alice, 99, 1, 3), EventTicket.MintRejection.UnknownTier);
        _expectRejected(_order(alice, VIP, 11, 4), EventTicket.MintRejection.SupplyCapExceeded);
        _expectRejected(_order(alice, VIP, 3, 5), EventTicket.MintRejection.MaxPerWalletExceeded);
        assertEq(ticket.totalMinted(), 0);
        assertFalse(ticket.nonceUsed(alice, 5));
    }

    function test_MintBatch_RevertWhen_Paused() public {
        vm.prank(pauser);
        ticket.pause();
        vm.expectRevert(PausableUpgradeable.EnforcedPause.selector);
        vm.prank(minter);
        ticket.mintBatch(_order(alice, GA, 1, 1));
    }

    // ----- mintWithVoucher ------------------------------------------------

    function test_MintWithVoucher_FreeTier() public {
        MintVoucher memory v = _voucher(alice, FREE, 2, 1);
        bytes memory sig = _signVoucher(v);
        vm.prank(stranger);
        uint256 first = ticket.mintWithVoucher(v, sig);

        assertEq(first, 1);
        assertEq(ticket.balanceOf(alice), 2);
        assertEq(usdc.balanceOf(address(escrow)), 0);
    }

    function test_MintWithVoucher_PaidByCaller() public {
        usdc.mint(alice, GA_PRICE * 2);
        vm.prank(alice);
        usdc.approve(address(escrow), GA_PRICE * 2);

        MintVoucher memory v = _voucher(alice, GA, 2, 1);
        bytes memory sig = _signVoucher(v);
        vm.prank(alice);
        ticket.mintWithVoucher(v, sig);

        assertEq(ticket.balanceOf(alice), 2);
        assertEq(usdc.balanceOf(address(escrow)), GA_PRICE * 2);
        assertEq(escrow.paidFor(address(ticket), 1), GA_PRICE);
        assertEq(escrow.paidFor(address(ticket), 2), GA_PRICE);
    }

    function test_MintWithVoucher_ThirdPartyCanPayForBuyer() public {
        usdc.mint(bob, GA_PRICE);
        vm.prank(bob);
        usdc.approve(address(escrow), GA_PRICE);

        MintVoucher memory v = _voucher(alice, GA, 1, 1);
        bytes memory sig = _signVoucher(v);
        vm.prank(bob);
        ticket.mintWithVoucher(v, sig);

        assertEq(ticket.ownerOf(1), alice);
        assertEq(usdc.balanceOf(bob), 0);
    }

    function test_MintWithVoucher_RevertWhen_PayerHasNoAllowance() public {
        MintVoucher memory v = _voucher(alice, GA, 1, 1);
        bytes memory sig = _signVoucher(v);
        vm.expectRevert();
        vm.prank(alice);
        ticket.mintWithVoucher(v, sig);
        assertEq(ticket.totalMinted(), 0);
    }

    function test_MintWithVoucher_RevertWhen_Expired() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(v);
        vm.warp(v.deadline + 1);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.VoucherExpired.selector, v.deadline));
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_RevertWhen_Replayed() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 42);
        bytes memory sig = _signVoucher(v);
        ticket.mintWithVoucher(v, sig);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.VoucherAlreadyUsed.selector, alice, 42));
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_RevertWhen_WrongSigner() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(ticket, v, aliceKey);
        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_RevertWhen_Tampered() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(v);
        v.quantity = 5;
        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_RevertWhen_SignedForAnotherEvent() public {
        EventTicket other = _createEvent(keccak256("event-2"), "Other");
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(other, v, signerKey);
        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_RevertWhen_SignerDisabled() public {
        vm.prank(admin);
        ticket.setVoucherSigner(address(0));
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(v);
        vm.expectRevert(EventTicket.InvalidVoucherSignature.selector);
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_RevertWhen_InvalidOrder() public {
        _expectVoucherRevert(_voucher(address(0), FREE, 1, 1), abi.encodeWithSelector(EventTicket.ZeroAddress.selector));
        _expectVoucherRevert(_voucher(alice, FREE, 0, 1), abi.encodeWithSelector(EventTicket.ZeroQuantity.selector));
        _expectVoucherRevert(_voucher(alice, 99, 1, 1), abi.encodeWithSelector(EventTicket.UnknownTier.selector, 99));
        _expectVoucherRevert(
            _voucher(alice, FREE, 51, 1), abi.encodeWithSelector(EventTicket.SupplyCapExceeded.selector, FREE)
        );
        _expectVoucherRevert(
            _voucher(alice, VIP, 3, 1), abi.encodeWithSelector(EventTicket.MaxPerWalletExceeded.selector, VIP, alice)
        );
    }

    function test_MintWithVoucher_RevertWhen_Paused() public {
        vm.prank(pauser);
        ticket.pause();
        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        bytes memory sig = _signVoucher(v);
        vm.expectRevert(PausableUpgradeable.EnforcedPause.selector);
        ticket.mintWithVoucher(v, sig);
    }

    function test_MintWithVoucher_AcceptsERC1271Signer() public {
        Mock1271Wallet wallet = new Mock1271Wallet(voucherSigner);
        vm.prank(admin);
        ticket.setVoucherSigner(address(wallet));

        MintVoucher memory v = _voucher(alice, FREE, 1, 1);
        ticket.mintWithVoucher(v, _signVoucher(v));
        assertEq(ticket.ownerOf(1), alice);
    }

    function test_MaxPerWallet_EnforcedAcrossBothMintPaths() public {
        vm.prank(minter);
        ticket.mintBatch(_order(alice, VIP, 1, 1));
        _purchase(aliceKey, VIP, 1, 2);

        MintVoucher memory v = _voucher(alice, VIP, 1, 3);
        bytes memory sig = _signVoucher(v);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.MaxPerWalletExceeded.selector, VIP, alice));
        ticket.mintWithVoucher(v, sig);
    }

    function test_Nonce_SharedBetweenMintPaths() public {
        MintVoucher memory v = _voucher(alice, FREE, 1, 9);
        ticket.mintWithVoucher(v, _signVoucher(v));

        vm.expectEmit(true, true, false, true, address(ticket));
        emit OrderRejected(alice, 9, EventTicket.MintRejection.NonceUsed);
        vm.prank(minter);
        ticket.mintBatch(_order(alice, FREE, 1, 9));
        assertEq(ticket.balanceOf(alice), 1);

        vm.prank(minter);
        ticket.mintBatch(_order(bob, FREE, 1, 10));
        MintVoucher memory v2 = _voucher(bob, FREE, 1, 10);
        bytes memory sig2 = _signVoucher(v2);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.VoucherAlreadyUsed.selector, bob, 10));
        ticket.mintWithVoucher(v2, sig2);
    }

    // ----- transfer lock --------------------------------------------------

    function test_Transfer_RevertWhen_Locked() public {
        _mintFree(alice, 1);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.TransferLocked.selector, 1, unlockTime));
        vm.prank(alice);
        ticket.transferFrom(alice, bob, 1);
        assertFalse(ticket.isTransferable(1, alice));
        assertTrue(ticket.isTransferable(1, address(resale)));
    }

    function test_Transfer_AllowedAfterUnlock() public {
        _mintFree(alice, 1);
        vm.warp(unlockTime);
        assertTrue(ticket.isTransferable(1, alice));
        vm.prank(alice);
        ticket.transferFrom(alice, bob, 1);
        assertEq(ticket.ownerOf(1), bob);
    }

    function test_Transfer_RevertWhen_RedeemedEvenAfterUnlock() public {
        _mintFree(alice, 1);
        _redeemOnChain(ticket, _single(1), 0);
        vm.warp(unlockTime + 1);

        assertFalse(ticket.isTransferable(1, alice));
        assertFalse(ticket.isTransferable(1, address(resale)));
        vm.expectRevert(abi.encodeWithSelector(EventTicket.Soulbound.selector, 1));
        vm.prank(alice);
        ticket.transferFrom(alice, bob, 1);
    }

    function test_Transfer_RevertWhen_Paused() public {
        _mintFree(alice, 1);
        vm.warp(unlockTime);
        vm.prank(pauser);
        ticket.pause();
        assertFalse(ticket.isTransferable(1, alice));
        vm.expectRevert(PausableUpgradeable.EnforcedPause.selector);
        vm.prank(alice);
        ticket.transferFrom(alice, bob, 1);

        vm.prank(pauser);
        ticket.unpause();
        vm.prank(alice);
        ticket.transferFrom(alice, bob, 1);
    }

    function test_Pause_RevertWhen_NotPauser() public {
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, ticket.PAUSER_ROLE()
            )
        );
        vm.prank(stranger);
        ticket.pause();

        vm.prank(pauser);
        ticket.pause();
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, ticket.PAUSER_ROLE()
            )
        );
        vm.prank(stranger);
        ticket.unpause();
    }

    // ----- redemption checkpoints and badges ------------------------------

    function test_Checkpoint_AndClaimBadge() public {
        _mintFree(alice, 3);
        uint256[] memory ids = new uint256[](3);
        (ids[0], ids[1], ids[2]) = (1, 2, 3);
        (bytes32 root, bytes32[] memory proof) = _merkle(ids, 1);

        vm.prank(redeemer);
        ticket.checkpointRedemptions(root, 3);
        assertEq(ticket.redemptionRoot(), root);
        assertEq(ticket.redemptionCount(), 3);
        assertEq(ticket.checkpointCount(), 1);

        vm.expectEmit(true, true, false, false, address(ticket));
        emit Redeemed(2, alice);
        vm.prank(stranger);
        ticket.claimAttendanceBadge(2, proof);

        assertTrue(ticket.isRedeemed(2));
        assertFalse(ticket.isRedeemed(1));
        assertEq(ticket.badgesClaimed(), 1);
    }

    function test_Checkpoint_RevertWhen_NotRedeemer() public {
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, ticket.REDEEMER_ROLE()
            )
        );
        vm.prank(stranger);
        ticket.checkpointRedemptions(bytes32(uint256(1)), 0);
    }

    function test_Checkpoint_RevertWhen_Invalid() public {
        _mintFree(alice, 2);
        vm.startPrank(redeemer);
        vm.expectRevert(EventTicket.InvalidCheckpoint.selector);
        ticket.checkpointRedemptions(bytes32(0), 1);

        vm.expectRevert(EventTicket.InvalidCheckpoint.selector);
        ticket.checkpointRedemptions(bytes32(uint256(1)), 3);

        ticket.checkpointRedemptions(bytes32(uint256(1)), 2);
        vm.expectRevert(EventTicket.InvalidCheckpoint.selector);
        ticket.checkpointRedemptions(bytes32(uint256(2)), 1);
        vm.stopPrank();
    }

    function test_ClaimBadge_RevertWhen_InvalidProof() public {
        _mintFree(alice, 2);
        (bytes32 root,) = _merkle(_single(1), 0);
        vm.prank(redeemer);
        ticket.checkpointRedemptions(root, 1);

        vm.expectRevert(EventTicket.InvalidProof.selector);
        ticket.claimAttendanceBadge(2, new bytes32[](0));
    }

    function test_ClaimBadge_RevertWhen_NoCheckpointYet() public {
        _mintFree(alice, 1);
        vm.expectRevert(EventTicket.InvalidProof.selector);
        ticket.claimAttendanceBadge(1, new bytes32[](0));
    }

    function test_ClaimBadge_RevertWhen_AlreadyRedeemed() public {
        _mintFree(alice, 1);
        _redeemOnChain(ticket, _single(1), 0);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.AlreadyRedeemed.selector, 1));
        ticket.claimAttendanceBadge(1, new bytes32[](0));
    }

    function test_ClaimBadge_RevertWhen_TokenDoesNotExist() public {
        vm.expectRevert(abi.encodeWithSelector(IERC721Errors.ERC721NonexistentToken.selector, 5));
        ticket.claimAttendanceBadge(5, new bytes32[](0));
    }

    function test_ClaimBadge_RevertWhen_TicketIsListed() public {
        uint256 id = _purchase(aliceKey, GA, 1, 1);
        vm.startPrank(alice);
        ticket.approve(address(resale), id);
        resale.list(address(ticket), id, GA_PRICE);
        vm.stopPrank();

        (bytes32 root, bytes32[] memory proof) = _merkle(_single(id), 0);
        vm.prank(redeemer);
        ticket.checkpointRedemptions(root, 1);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.TicketListed.selector, id));
        ticket.claimAttendanceBadge(id, proof);
    }

    // ----- administration -------------------------------------------------

    function test_ConfigureTier_AddsAndUpdates() public {
        vm.startPrank(admin);
        ticket.configureTier(4, "Balcony", 40e6, 20, 1);
        ticket.configureTier(GA, "General", 30e6, 120, 6);
        vm.stopPrank();

        assertEq(ticket.tierPrice(4), 40e6);
        (string memory name, uint256 price, uint256 cap,, uint256 maxPerWallet) = ticket.tierConfig(GA);
        assertEq(name, "General");
        assertEq(price, 30e6);
        assertEq(cap, 120);
        assertEq(maxPerWallet, 6);
    }

    function test_ConfigureTier_RevertWhen_NotAdmin() public {
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, ticket.DEFAULT_ADMIN_ROLE()
            )
        );
        vm.prank(stranger);
        ticket.configureTier(4, "x", 1, 1, 1);
    }

    function test_ConfigureTier_RevertWhen_Invalid() public {
        _mintFree(alice, 3);
        vm.startPrank(admin);
        vm.expectRevert(EventTicket.InvalidTierConfig.selector);
        ticket.configureTier(FREE, "x", 0, 2, 0);
        vm.expectRevert(EventTicket.InvalidTierConfig.selector);
        ticket.configureTier(uint256(type(uint64).max) + 1, "x", 0, 1, 0);
        vm.expectRevert(EventTicket.InvalidTierConfig.selector);
        ticket.configureTier(5, "x", uint256(type(uint128).max) + 1, 1, 0);
        vm.expectRevert(EventTicket.InvalidTierConfig.selector);
        ticket.configureTier(5, "x", 0, uint256(type(uint32).max) + 1, 0);
        vm.expectRevert(EventTicket.InvalidTierConfig.selector);
        ticket.configureTier(5, "x", 0, 1, uint256(type(uint32).max) + 1);
        vm.stopPrank();
    }

    function test_AdminSetters() public {
        vm.startPrank(admin);
        ticket.setUnlockTime(123);
        ticket.setVoucherSigner(bob);
        ticket.setResaleContract(stranger);
        vm.stopPrank();
        assertEq(ticket.unlockTime(), 123);
        assertEq(ticket.voucherSigner(), bob);
        assertEq(ticket.resaleContract(), stranger);
    }

    function test_AdminSetters_RevertWhen_NotAdmin() public {
        bytes memory err = abi.encodeWithSelector(
            IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, ticket.DEFAULT_ADMIN_ROLE()
        );
        vm.startPrank(stranger);
        vm.expectRevert(err);
        ticket.setUnlockTime(1);
        vm.expectRevert(err);
        ticket.setVoucherSigner(stranger);
        vm.expectRevert(err);
        ticket.setResaleContract(stranger);
        vm.stopPrank();
    }

    function test_DefaultAdmin_TransferIsTwoStepAndDelayed() public {
        vm.prank(admin);
        ticket.beginDefaultAdminTransfer(bob);

        vm.expectRevert();
        vm.prank(bob);
        ticket.acceptDefaultAdminTransfer();

        vm.warp(block.timestamp + ADMIN_DELAY + 1);
        vm.prank(bob);
        ticket.acceptDefaultAdminTransfer();
        assertEq(ticket.defaultAdmin(), bob);
        assertFalse(ticket.hasRole(ticket.DEFAULT_ADMIN_ROLE(), admin));
    }

    // ----- views and metadata ---------------------------------------------

    function test_Views_RevertWhen_Unknown() public {
        vm.expectRevert(abi.encodeWithSelector(EventTicket.UnknownTier.selector, 77));
        ticket.tierConfig(77);
        vm.expectRevert(abi.encodeWithSelector(EventTicket.UnknownTier.selector, 77));
        ticket.tierPrice(77);
        vm.expectRevert(abi.encodeWithSelector(IERC721Errors.ERC721NonexistentToken.selector, 1));
        ticket.tierOf(1);
        vm.expectRevert(abi.encodeWithSelector(IERC721Errors.ERC721NonexistentToken.selector, 1));
        ticket.tokenURI(1);
    }

    function test_SupportsInterface() public view {
        assertTrue(ticket.supportsInterface(type(IERC721).interfaceId));
        assertTrue(ticket.supportsInterface(type(IERC2981).interfaceId));
        assertTrue(ticket.supportsInterface(type(IAccessControl).interfaceId));
        assertFalse(ticket.supportsInterface(0xdeadbeef));
    }

    function test_RoyaltyInfo_PaysOrganizer() public {
        _mintFree(alice, 1);
        (address receiver, uint256 amount) = ticket.royaltyInfo(1, 200e6);
        assertEq(receiver, organizer);
        assertEq(amount, 10e6);
    }

    function test_TokenURI_IsFullyOnChainAndReflectsRedemption() public {
        _mintFree(alice, 1);
        bytes memory json = _decodeTokenJson(1);
        assertTrue(TextUtils.contains(json, bytes('"name":"Neon Arches #1"')));
        assertTrue(TextUtils.contains(json, bytes('"trait_type":"Tier","value":"Guest List"')));
        assertTrue(TextUtils.contains(json, bytes('"trait_type":"Status","value":"Valid"')));
        assertTrue(TextUtils.contains(json, bytes("data:image/svg+xml;base64,")));

        _redeemOnChain(ticket, _single(1), 0);
        json = _decodeTokenJson(1);
        assertTrue(TextUtils.contains(json, bytes('"trait_type":"Status","value":"Attended"')));
        assertTrue(TextUtils.contains(json, bytes("Soulbound proof of attendance")));
    }

    function test_HashVoucher_MatchesEIP712Spec() public view {
        MintVoucher memory v = _voucher(alice, GA, 2, 5);
        bytes32 domainSeparator = keccak256(
            abi.encode(
                keccak256("EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)"),
                keccak256("TixFlow EventTicket"),
                keccak256("1"),
                block.chainid,
                address(ticket)
            )
        );
        bytes32 structHash =
            keccak256(abi.encode(ticket.MINT_VOUCHER_TYPEHASH(), v.buyer, v.tierId, v.quantity, v.nonce, v.deadline));
        assertEq(ticket.hashVoucher(v), keccak256(abi.encodePacked("\x19\x01", domainSeparator, structHash)));
    }

    // ----- helpers --------------------------------------------------------

    function _init() private view returns (TicketInit memory) {
        return TicketInit({
            name: "Clone",
            symbol: "CLN",
            admin: admin,
            adminDelay: ADMIN_DELAY,
            organizer: organizer,
            minter: minter,
            redeemer: redeemer,
            pauser: pauser,
            voucherSigner: voucherSigner,
            resaleContract: address(resale),
            escrow: address(escrow),
            renderer: address(renderer),
            eventStart: eventStart,
            eventEnd: eventEnd,
            unlockTime: unlockTime,
            royaltyBps: ROYALTY_BPS,
            tiers: _defaultTiers()
        });
    }

    function _expectInitRevert(TicketInit memory p, bytes4 selector) private {
        EventTicket t = EventTicket(Clones.clone(address(implementation)));
        vm.expectRevert(selector);
        t.initialize(p);
    }

    function _expectRejected(MintOrder[] memory orders, EventTicket.MintRejection reason) private {
        vm.expectEmit(true, true, false, true, address(ticket));
        emit OrderRejected(orders[0].buyer, orders[0].nonce, reason);
        vm.prank(minter);
        ticket.mintBatch(orders);
    }

    function _expectVoucherRevert(MintVoucher memory v, bytes memory err) private {
        bytes memory sig = _signVoucher(v);
        vm.expectRevert(err);
        ticket.mintWithVoucher(v, sig);
    }

    function _mintFree(address to, uint256 quantity) private {
        uint256 nonce = uint256(keccak256(abi.encode(to, quantity, ticket.totalMinted())));
        vm.prank(minter);
        ticket.mintBatch(_order(to, FREE, quantity, nonce));
    }

    function _decodeTokenJson(uint256 tokenId) private view returns (bytes memory) {
        string memory uri = ticket.tokenURI(tokenId);
        return TextUtils.decodeBase64(TextUtils.stripPrefix(uri, "data:application/json;base64,"));
    }
}
