// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";

import {EventTicket} from "../../src/EventTicket.sol";
import {TicketRenderer} from "../../src/TicketRenderer.sol";
import {TicketFactory} from "../../src/TicketFactory.sol";
import {EventEscrow} from "../../src/EventEscrow.sol";
import {PriceCappedResale} from "../../src/PriceCappedResale.sol";
import {IEventEscrow} from "../../src/interfaces/IEventEscrow.sol";
import {MockUSDC} from "../../src/mocks/MockUSDC.sol";
import {
    MintVoucher,
    MintOrder,
    TierInit,
    PaymentAuthorization,
    PermitSignature
} from "../../src/TixFlowTypes.sol";

/// @notice Deploys the full protocol and offers signing helpers shared by every test contract.
abstract contract BaseTest is Test {
    uint256 internal constant START = 1_760_000_000;
    uint48 internal constant ADMIN_DELAY = 1 days;
    bytes32 internal constant EVENT_ID = keccak256("event-1");

    uint256 internal constant GA = 1;
    uint256 internal constant VIP = 2;
    uint256 internal constant FREE = 3;
    uint256 internal constant GA_PRICE = 25e6;
    uint256 internal constant VIP_PRICE = 100e6;
    uint96 internal constant ROYALTY_BPS = 500;

    MockUSDC internal usdc;
    TicketRenderer internal renderer;
    EventTicket internal implementation;
    EventEscrow internal escrow;
    PriceCappedResale internal resale;
    TicketFactory internal factory;
    EventTicket internal ticket;

    address internal admin = makeAddr("admin");
    address internal minter = makeAddr("minter");
    address internal redeemer = makeAddr("redeemer");
    address internal pauser = makeAddr("pauser");
    address internal organizer = makeAddr("organizer");
    address internal relayer = makeAddr("relayer");
    address internal stranger = makeAddr("stranger");

    uint256 internal signerKey = uint256(keccak256("voucher-signer"));
    address internal voucherSigner = vm.addr(signerKey);
    uint256 internal aliceKey = uint256(keccak256("alice"));
    address internal alice = vm.addr(aliceKey);
    uint256 internal bobKey = uint256(keccak256("bob"));
    address internal bob = vm.addr(bobKey);

    uint64 internal eventStart;
    uint64 internal eventEnd;
    uint64 internal unlockTime;

    function setUp() public virtual {
        vm.warp(START);
        eventStart = uint64(START + 7 days);
        eventEnd = eventStart + 6 hours;
        unlockTime = eventEnd;

        usdc = new MockUSDC();
        renderer = new TicketRenderer();
        implementation = new EventTicket();

        address predictedFactory = vm.computeCreateAddress(address(this), vm.getNonce(address(this)) + 2);
        escrow = new EventEscrow(IERC20(address(usdc)), admin, ADMIN_DELAY, predictedFactory, minter);
        resale = new PriceCappedResale(
            IERC20(address(usdc)), IEventEscrow(address(escrow)), admin, ADMIN_DELAY, 1000, predictedFactory
        );
        factory = new TicketFactory(
            address(implementation), address(escrow), address(resale), address(renderer), _platformConfig(), minter
        );
        assertEq(address(factory), predictedFactory, "factory prediction");

        ticket = _createEvent(EVENT_ID, "Neon Arches");
    }

    function _platformConfig() internal view returns (TicketFactory.PlatformConfig memory) {
        return TicketFactory.PlatformConfig({
            admin: admin,
            adminDelay: ADMIN_DELAY,
            minter: minter,
            redeemer: redeemer,
            pauser: pauser,
            voucherSigner: voucherSigner
        });
    }

    function _defaultTiers() internal pure returns (TierInit[] memory tiers) {
        tiers = new TierInit[](3);
        tiers[0] = TierInit({tierId: GA, name: "General Admission", price: GA_PRICE, supplyCap: 100, maxPerWallet: 4});
        tiers[1] = TierInit({tierId: VIP, name: "VIP", price: VIP_PRICE, supplyCap: 10, maxPerWallet: 2});
        tiers[2] = TierInit({tierId: FREE, name: "Guest List", price: 0, supplyCap: 50, maxPerWallet: 0});
    }

    function _createParams(bytes32 eventId, string memory name)
        internal
        view
        returns (TicketFactory.CreateEventParams memory)
    {
        return TicketFactory.CreateEventParams({
            eventId: eventId,
            name: name,
            symbol: "TIX",
            organizer: organizer,
            eventStart: eventStart,
            eventEnd: eventEnd,
            unlockTime: unlockTime,
            royaltyBps: ROYALTY_BPS,
            tiers: _defaultTiers()
        });
    }

    function _createEvent(bytes32 eventId, string memory name) internal returns (EventTicket) {
        vm.prank(minter);
        return EventTicket(factory.createEvent(_createParams(eventId, name)));
    }

    // ----- vouchers -------------------------------------------------------

    function _voucher(address buyer, uint256 tierId, uint256 quantity, uint256 nonce)
        internal
        view
        returns (MintVoucher memory)
    {
        return MintVoucher({
            buyer: buyer,
            tierId: tierId,
            quantity: quantity,
            nonce: nonce,
            deadline: block.timestamp + 1 hours
        });
    }

    function _signVoucher(EventTicket t, MintVoucher memory voucher, uint256 key) internal view returns (bytes memory) {
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(key, t.hashVoucher(voucher));
        return abi.encodePacked(r, s, v);
    }

    function _signVoucher(MintVoucher memory voucher) internal view returns (bytes memory) {
        return _signVoucher(ticket, voucher, signerKey);
    }

    function _order(address buyer, uint256 tierId, uint256 quantity, uint256 nonce)
        internal
        pure
        returns (MintOrder[] memory orders)
    {
        orders = new MintOrder[](1);
        orders[0] = MintOrder({buyer: buyer, tierId: tierId, quantity: quantity, nonce: nonce});
    }

    // ----- USDC signatures ------------------------------------------------

    function _usdcDigest(bytes32 structHash) private view returns (bytes32) {
        return keccak256(abi.encodePacked("\x19\x01", usdc.DOMAIN_SEPARATOR(), structHash));
    }

    function _signReceive(uint256 key, address to, uint256 value, bytes32 nonce)
        internal
        view
        returns (PaymentAuthorization memory auth)
    {
        auth.validAfter = 0;
        auth.validBefore = block.timestamp + 1 days;
        auth.nonce = nonce;
        bytes32 structHash = keccak256(
            abi.encode(
                usdc.RECEIVE_WITH_AUTHORIZATION_TYPEHASH(),
                vm.addr(key),
                to,
                value,
                auth.validAfter,
                auth.validBefore,
                nonce
            )
        );
        (auth.v, auth.r, auth.s) = vm.sign(key, _usdcDigest(structHash));
    }

    function _signPermit(uint256 key, address spender, uint256 value) internal view returns (PermitSignature memory p) {
        address owner = vm.addr(key);
        p.deadline = block.timestamp + 1 days;
        bytes32 structHash =
            keccak256(abi.encode(usdc.PERMIT_TYPEHASH(), owner, spender, value, usdc.nonces(owner), p.deadline));
        (p.v, p.r, p.s) = vm.sign(key, _usdcDigest(structHash));
    }

    // ----- flows ----------------------------------------------------------

    /// @dev Funds the buyer and buys through the gasless path, relayed by `relayer`.
    function _purchase(EventTicket t, uint256 buyerKey, uint256 tierId, uint256 quantity, uint256 nonce)
        internal
        returns (uint256 firstTokenId)
    {
        address buyer = vm.addr(buyerKey);
        uint256 cost = t.tierPrice(tierId) * quantity;
        if (cost != 0) usdc.mint(buyer, cost);

        MintVoucher memory voucher = _voucher(buyer, tierId, quantity, nonce);
        bytes memory signature = _signVoucher(t, voucher, signerKey);
        PaymentAuthorization memory auth =
            _signReceive(buyerKey, address(escrow), cost, keccak256(abi.encode(address(t), nonce)));

        vm.prank(relayer);
        return escrow.purchaseWithAuthorization(address(t), voucher, signature, auth);
    }

    function _purchase(uint256 buyerKey, uint256 tierId, uint256 quantity, uint256 nonce) internal returns (uint256) {
        return _purchase(ticket, buyerKey, tierId, quantity, nonce);
    }

    // ----- Merkle helpers (sorted-pair hashing, compatible with OpenZeppelin MerkleProof) -----

    function _leaf(uint256 tokenId) internal pure returns (bytes32) {
        return keccak256(bytes.concat(keccak256(abi.encode(tokenId))));
    }

    function _hashPair(bytes32 a, bytes32 b) private pure returns (bytes32) {
        return a < b ? keccak256(abi.encodePacked(a, b)) : keccak256(abi.encodePacked(b, a));
    }

    /// @dev Returns the root of the tree over `tokenIds` and the proof for `tokenIds[index]`.
    function _merkle(uint256[] memory tokenIds, uint256 index)
        internal
        pure
        returns (bytes32 root, bytes32[] memory proof)
    {
        uint256 n = tokenIds.length;
        bytes32[] memory level = new bytes32[](n);
        for (uint256 i = 0; i < n; ++i) {
            level[i] = _leaf(tokenIds[i]);
        }

        bytes32[] memory scratch = new bytes32[](32);
        uint256 proofLength;
        while (n > 1) {
            uint256 sibling = index ^ 1;
            if (sibling < n) scratch[proofLength++] = level[sibling];

            uint256 next = (n + 1) / 2;
            for (uint256 i = 0; i < next; ++i) {
                level[i] = (2 * i + 1 < n) ? _hashPair(level[2 * i], level[2 * i + 1]) : level[2 * i];
            }
            n = next;
            index /= 2;
        }

        root = level[0];
        proof = new bytes32[](proofLength);
        for (uint256 i = 0; i < proofLength; ++i) {
            proof[i] = scratch[i];
        }
    }

    function _single(uint256 tokenId) internal pure returns (uint256[] memory ids) {
        ids = new uint256[](1);
        ids[0] = tokenId;
    }

    /// @dev Checkpoints exactly `tokenIds` as redeemed and claims the badge for `tokenIds[index]`.
    function _redeemOnChain(EventTicket t, uint256[] memory tokenIds, uint256 index) internal {
        (bytes32 root, bytes32[] memory proof) = _merkle(tokenIds, index);
        vm.prank(redeemer);
        t.checkpointRedemptions(root, tokenIds.length);
        t.claimAttendanceBadge(tokenIds[index], proof);
    }
}
