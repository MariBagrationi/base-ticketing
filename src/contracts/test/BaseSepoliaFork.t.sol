// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";

import {EventTicket} from "../src/EventTicket.sol";
import {TicketRenderer} from "../src/TicketRenderer.sol";
import {TicketFactory} from "../src/TicketFactory.sol";
import {EventEscrow} from "../src/EventEscrow.sol";
import {PriceCappedResale} from "../src/PriceCappedResale.sol";
import {IEventEscrow} from "../src/interfaces/IEventEscrow.sol";
import {MintVoucher, TierInit, PaymentAuthorization} from "../src/TixFlowTypes.sol";

interface IFiatToken is IERC20 {
    function DOMAIN_SEPARATOR() external view returns (bytes32);
    function decimals() external view returns (uint8);
}

/// @notice Runs the gasless purchase, refund and release flows against the real USDC on Base Sepolia.
/// @dev Skipped unless BASE_SEPOLIA_RPC_URL is set:
///      BASE_SEPOLIA_RPC_URL=https://sepolia.base.org forge test --match-contract BaseSepoliaFork
contract BaseSepoliaForkTest is Test {
    address private constant USDC = 0x036CbD53842c5426634e7929541eC2318f3dCF7e;
    bytes32 private constant RECEIVE_WITH_AUTHORIZATION_TYPEHASH = keccak256(
        "ReceiveWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)"
    );
    uint256 private constant PRICE = 25e6;

    IFiatToken private usdc = IFiatToken(USDC);
    EventEscrow private escrow;
    EventTicket private ticket;

    address private admin = makeAddr("admin");
    address private organizer = makeAddr("organizer");
    uint256 private signerKey = uint256(keccak256("fork-signer"));
    uint256 private buyerKey = uint256(keccak256("fork-buyer"));
    address private buyer = vm.addr(buyerKey);
    bool private forked;

    function setUp() public {
        string memory rpc = vm.envOr("BASE_SEPOLIA_RPC_URL", string(""));
        if (bytes(rpc).length == 0) return;
        vm.createSelectFork(rpc);
        forked = true;

        address predictedFactory = vm.computeCreateAddress(address(this), vm.getNonce(address(this)) + 4);
        TicketRenderer renderer = new TicketRenderer();
        EventTicket implementation = new EventTicket();
        escrow = new EventEscrow(usdc, admin, 0, predictedFactory, admin);
        PriceCappedResale resale =
            new PriceCappedResale(usdc, IEventEscrow(address(escrow)), admin, 0, 1000, predictedFactory);
        TicketFactory factory = new TicketFactory(
            address(implementation),
            address(escrow),
            address(resale),
            address(renderer),
            TicketFactory.PlatformConfig(admin, 0, admin, admin, admin, vm.addr(signerKey)),
            address(this)
        );
        assertEq(address(factory), predictedFactory);

        TierInit[] memory tiers = new TierInit[](1);
        tiers[0] = TierInit(1, "General Admission", PRICE, 100, 4);
        ticket = EventTicket(
            factory.createEvent(
                TicketFactory.CreateEventParams({
                    eventId: keccak256("fork-event"),
                    name: "Fork Event",
                    symbol: "FORK",
                    organizer: organizer,
                    eventStart: uint64(block.timestamp + 1 days),
                    eventEnd: uint64(block.timestamp + 2 days),
                    unlockTime: uint64(block.timestamp + 2 days),
                    royaltyBps: 500,
                    tiers: tiers
                })
            )
        );
        deal(USDC, buyer, PRICE * 2);
    }

    function test_Fork_RealUsdcHasSixDecimals() public {
        if (_skipUnlessForked()) return;
        assertEq(usdc.decimals(), 6);
        assertEq(usdc.balanceOf(buyer), PRICE * 2);
    }

    function test_Fork_GaslessPurchaseThenRelease() public {
        if (_skipUnlessForked()) return;
        _purchase(2, 1);

        assertEq(ticket.balanceOf(buyer), 2);
        assertEq(usdc.balanceOf(buyer), 0);
        assertEq(usdc.balanceOf(address(escrow)), PRICE * 2);

        vm.warp(ticket.eventEnd());
        escrow.release(address(ticket));
        assertEq(usdc.balanceOf(organizer), PRICE * 2);
    }

    function test_Fork_CancelledEventRefundsRealUsdc() public {
        if (_skipUnlessForked()) return;
        uint256 first = _purchase(1, 1);

        vm.prank(organizer);
        escrow.cancelEvent(address(ticket));
        escrow.claimRefund(address(ticket), first);
        assertEq(usdc.balanceOf(buyer), PRICE * 2);
        assertEq(escrow.totalHeld(), 0);
    }

    function _skipUnlessForked() private returns (bool) {
        if (!forked) vm.skip(true);
        return !forked;
    }

    function _purchase(uint256 quantity, uint256 nonce) private returns (uint256) {
        MintVoucher memory voucher = MintVoucher(buyer, 1, quantity, nonce, block.timestamp + 1 hours);
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(signerKey, ticket.hashVoucher(voucher));

        PaymentAuthorization memory auth;
        auth.validBefore = block.timestamp + 1 hours;
        auth.nonce = bytes32(nonce);
        bytes32 structHash = keccak256(
            abi.encode(
                RECEIVE_WITH_AUTHORIZATION_TYPEHASH,
                buyer,
                address(escrow),
                PRICE * quantity,
                auth.validAfter,
                auth.validBefore,
                auth.nonce
            )
        );
        (auth.v, auth.r, auth.s) =
            vm.sign(buyerKey, keccak256(abi.encodePacked("\x19\x01", usdc.DOMAIN_SEPARATOR(), structHash)));

        return escrow.purchaseWithAuthorization(address(ticket), voucher, abi.encodePacked(r, s, v), auth);
    }
}
