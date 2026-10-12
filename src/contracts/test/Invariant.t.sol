// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";

import {BaseTest} from "./utils/BaseTest.sol";
import {EventTicket} from "../src/EventTicket.sol";
import {EventEscrow} from "../src/EventEscrow.sol";
import {PriceCappedResale} from "../src/PriceCappedResale.sol";
import {MockUSDC} from "../src/mocks/MockUSDC.sol";
import {MintVoucher, MintOrder, PaymentAuthorization} from "../src/TixFlowTypes.sol";

/// @notice Drives the protocol with random but valid-looking actions across two events and four actors.
contract Handler is Test {
    uint256 private constant ACTORS = 4;

    MockUSDC public usdc;
    EventEscrow public escrow;
    PriceCappedResale public resale;
    EventTicket[2] public tickets;

    address private minter;
    address private redeemer;
    address private organizer;
    uint256 private signerKey;
    uint256[ACTORS] private actorKeys;

    uint256 private nonce;
    uint256[] private openListings;

    // Ghost state the invariants compare the contracts against.
    uint256 public ghostDeposited;
    uint256 public ghostReleased;
    uint256 public ghostRefunded;
    mapping(address ticket => uint256[]) private redeemedIds;
    mapping(address ticket => mapping(uint256 tokenId => address)) public ownerAtRedemption;

    constructor(
        MockUSDC usdc_,
        EventEscrow escrow_,
        PriceCappedResale resale_,
        EventTicket ticketA,
        EventTicket ticketB,
        address minter_,
        address redeemer_,
        address organizer_,
        uint256 signerKey_
    ) {
        usdc = usdc_;
        escrow = escrow_;
        resale = resale_;
        tickets[0] = ticketA;
        tickets[1] = ticketB;
        minter = minter_;
        redeemer = redeemer_;
        organizer = organizer_;
        signerKey = signerKey_;
        for (uint256 i = 0; i < ACTORS; ++i) {
            actorKeys[i] = uint256(keccak256(abi.encode("actor", i)));
        }
    }

    function actor(uint256 seed) public view returns (address) {
        return vm.addr(actorKeys[seed % ACTORS]);
    }

    function actorCount() external pure returns (uint256) {
        return ACTORS;
    }

    function redeemedCount(address ticket) external view returns (uint256) {
        return redeemedIds[ticket].length;
    }

    function redeemedAt(address ticket, uint256 index) external view returns (uint256) {
        return redeemedIds[ticket][index];
    }

    // ----- actions --------------------------------------------------------

    function purchase(uint256 eventSeed, uint256 actorSeed, uint256 tierSeed, uint256 quantity) external {
        EventTicket t = tickets[eventSeed % 2];
        uint256 key = actorKeys[actorSeed % ACTORS];
        address buyer = vm.addr(key);
        uint256 tierId = bound(tierSeed, 1, 3);
        quantity = bound(quantity, 1, 5);
        uint256 cost = t.tierPrice(tierId) * quantity;
        if (cost != 0) usdc.mint(buyer, cost);

        MintVoucher memory v = MintVoucher(buyer, tierId, quantity, ++nonce, block.timestamp + 1 hours);
        (uint8 sv, bytes32 sr, bytes32 ss) = vm.sign(signerKey, t.hashVoucher(v));
        PaymentAuthorization memory auth = _authorize(key, cost);

        try escrow.purchaseWithAuthorization(address(t), v, abi.encodePacked(sr, ss, sv), auth) {
            ghostDeposited += cost;
        } catch {}
    }

    function mintComplimentary(uint256 eventSeed, uint256 actorSeed, uint256 tierSeed, uint256 quantity) external {
        EventTicket t = tickets[eventSeed % 2];
        MintOrder[] memory orders = new MintOrder[](1);
        orders[0] = MintOrder(actor(actorSeed), bound(tierSeed, 1, 3), bound(quantity, 0, 5), ++nonce);
        vm.prank(minter);
        try t.mintBatch(orders) {} catch {}
    }

    function redeem(uint256 eventSeed, uint256 tokenSeed) external {
        EventTicket t = tickets[eventSeed % 2];
        uint256 minted = t.totalMinted();
        if (minted == 0) return;
        uint256 tokenId = bound(tokenSeed, 1, minted);
        if (t.isRedeemed(tokenId) || t.ownerOf(tokenId) == address(resale)) return;

        // Checkpoint a single-leaf tree: the leaf is the root and the proof is empty.
        bytes32 root = keccak256(bytes.concat(keccak256(abi.encode(tokenId))));
        vm.prank(redeemer);
        try t.checkpointRedemptions(root, t.redemptionCount()) {} catch { return; }
        try t.claimAttendanceBadge(tokenId, new bytes32[](0)) {
            redeemedIds[address(t)].push(tokenId);
            ownerAtRedemption[address(t)][tokenId] = t.ownerOf(tokenId);
        } catch {}
    }

    function transfer(uint256 eventSeed, uint256 tokenSeed, uint256 toSeed) external {
        EventTicket t = tickets[eventSeed % 2];
        uint256 minted = t.totalMinted();
        if (minted == 0) return;
        uint256 tokenId = bound(tokenSeed, 1, minted);
        address holder = t.ownerOf(tokenId);
        vm.prank(holder);
        try t.transferFrom(holder, actor(toSeed), tokenId) {} catch {}
    }

    function list(uint256 eventSeed, uint256 tokenSeed, uint256 price) external {
        EventTicket t = tickets[eventSeed % 2];
        uint256 minted = t.totalMinted();
        if (minted == 0) return;
        uint256 tokenId = bound(tokenSeed, 1, minted);
        address holder = t.ownerOf(tokenId);
        price = bound(price, 1, 200e6);

        vm.startPrank(holder);
        t.approve(address(resale), tokenId);
        try resale.list(address(t), tokenId, price) returns (uint256 listingId) {
            openListings.push(listingId);
        } catch {}
        vm.stopPrank();
    }

    function buyListing(uint256 listingSeed, uint256 actorSeed) external {
        if (openListings.length == 0) return;
        uint256 listingId = openListings[listingSeed % openListings.length];
        (,,, uint256 price,) = resale.listings(listingId);
        address buyer = actor(actorSeed);
        usdc.mint(buyer, price);
        vm.startPrank(buyer);
        usdc.approve(address(resale), price);
        try resale.buy(listingId) {} catch {}
        vm.stopPrank();
    }

    function cancelListing(uint256 listingSeed) external {
        if (openListings.length == 0) return;
        uint256 listingId = openListings[listingSeed % openListings.length];
        (address seller,,,,) = resale.listings(listingId);
        vm.prank(seller);
        try resale.cancel(listingId) {} catch {}
    }

    function cancelEvent(uint256 eventSeed, uint256 chance) external {
        // Rare, so most runs keep exercising the live-event paths.
        if (chance % 20 != 0) return;
        vm.prank(organizer);
        try escrow.cancelEvent(address(tickets[eventSeed % 2])) {} catch {}
    }

    function refund(uint256 eventSeed, uint256 tokenSeed) external {
        EventTicket t = tickets[eventSeed % 2];
        uint256 minted = t.totalMinted();
        if (minted == 0) return;
        uint256 tokenId = bound(tokenSeed, 1, minted);
        uint256 amount = escrow.paidFor(address(t), tokenId);
        try escrow.claimRefund(address(t), tokenId) {
            ghostRefunded += amount;
        } catch {}
    }

    function release(uint256 eventSeed) external {
        address t = address(tickets[eventSeed % 2]);
        (,, uint256 held,) = escrow.eventAccount(t);
        try escrow.release(t) {
            ghostReleased += held;
        } catch {}
    }

    function warp(uint256 secondsForward) external {
        vm.warp(block.timestamp + bound(secondsForward, 1 hours, 3 days));
    }

    function _authorize(uint256 key, uint256 value) private view returns (PaymentAuthorization memory auth) {
        auth.validBefore = block.timestamp + 1 days;
        auth.nonce = bytes32(nonce);
        bytes32 structHash = keccak256(
            abi.encode(
                usdc.RECEIVE_WITH_AUTHORIZATION_TYPEHASH(),
                vm.addr(key),
                address(escrow),
                value,
                auth.validAfter,
                auth.validBefore,
                auth.nonce
            )
        );
        (auth.v, auth.r, auth.s) =
            vm.sign(key, keccak256(abi.encodePacked("\x19\x01", usdc.DOMAIN_SEPARATOR(), structHash)));
    }
}

contract InvariantTest is BaseTest {
    Handler private handler;
    EventTicket private ticketB;

    function setUp() public override {
        super.setUp();
        ticketB = _createEvent(keccak256("event-b"), "Second Event");
        handler =
            new Handler(usdc, escrow, resale, ticket, ticketB, minter, redeemer, organizer, signerKey);
        bytes4[] memory selectors = new bytes4[](11);
        selectors[0] = Handler.purchase.selector;
        selectors[1] = Handler.mintComplimentary.selector;
        selectors[2] = Handler.redeem.selector;
        selectors[3] = Handler.transfer.selector;
        selectors[4] = Handler.list.selector;
        selectors[5] = Handler.buyListing.selector;
        selectors[6] = Handler.cancelListing.selector;
        selectors[7] = Handler.cancelEvent.selector;
        selectors[8] = Handler.refund.selector;
        selectors[9] = Handler.release.selector;
        selectors[10] = Handler.warp.selector;
        targetSelector(FuzzSelector({addr: address(handler), selectors: selectors}));
        targetContract(address(handler));
    }

    /// @notice Tickets minted never exceed any tier's supply cap.
    function invariant_MintedNeverExceedsSupplyCap() public view {
        _assertSupply(ticket);
        _assertSupply(ticketB);
    }

    /// @notice No wallet ever holds more mints of a tier than the per-wallet limit allows.
    function invariant_MaxPerWalletNeverExceeded() public view {
        for (uint256 a = 0; a < handler.actorCount(); ++a) {
            for (uint256 tierId = GA; tierId <= VIP; ++tierId) {
                (,,,, uint256 maxPerWallet) = ticket.tierConfig(tierId);
                assertLe(ticket.mintedBy(tierId, handler.actor(a)), maxPerWallet);
                assertLe(ticketB.mintedBy(tierId, handler.actor(a)), maxPerWallet);
            }
        }
    }

    /// @notice The escrow holds exactly the sales that were neither released nor refunded.
    function invariant_EscrowBalanceMatchesUnsettledSales() public view {
        uint256 expected = handler.ghostDeposited() - handler.ghostReleased() - handler.ghostRefunded();
        (,, uint256 heldA,) = escrow.eventAccount(address(ticket));
        (,, uint256 heldB,) = escrow.eventAccount(address(ticketB));

        assertEq(escrow.totalHeld(), expected, "totalHeld vs ghost");
        assertEq(heldA + heldB, expected, "sum of events vs ghost");
        assertEq(usdc.balanceOf(address(escrow)), expected, "USDC balance vs ghost");
    }

    /// @notice Once redeemed, a ticket never changes hands again.
    function invariant_RedeemedTicketsNeverMove() public view {
        _assertSoulbound(ticket);
        _assertSoulbound(ticketB);
    }

    /// @notice The marketplace never owes more royalties and fees than it holds.
    function invariant_MarketplaceIsSolvent() public view {
        assertGe(usdc.balanceOf(address(resale)), resale.pendingWithdrawals(organizer));
    }

    function _assertSupply(EventTicket t) private view {
        uint256 total;
        for (uint256 tierId = GA; tierId <= FREE; ++tierId) {
            (,, uint256 cap, uint256 minted,) = t.tierConfig(tierId);
            assertLe(minted, cap);
            total += minted;
        }
        assertEq(t.totalMinted(), total);
    }

    function _assertSoulbound(EventTicket t) private view {
        uint256 count = handler.redeemedCount(address(t));
        for (uint256 i = 0; i < count; ++i) {
            uint256 tokenId = handler.redeemedAt(address(t), i);
            assertTrue(t.isRedeemed(tokenId));
            assertEq(t.ownerOf(tokenId), handler.ownerAtRedemption(address(t), tokenId));
        }
    }
}
