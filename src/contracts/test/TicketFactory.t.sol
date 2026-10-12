// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {console2} from "forge-std/console2.sol";
import {IAccessControl} from "@openzeppelin/contracts/access/IAccessControl.sol";

import {BaseTest} from "./utils/BaseTest.sol";
import {EventTicket} from "../src/EventTicket.sol";
import {TicketFactory} from "../src/TicketFactory.sol";

contract TicketFactoryTest is BaseTest {
    event EventCreated(
        bytes32 indexed eventId, address indexed ticket, address indexed organizer, uint64 eventStart, uint64 eventEnd
    );

    function test_Constructor_RevertWhen_ZeroAddress() public {
        TicketFactory.PlatformConfig memory cfg = _platformConfig();
        vm.expectRevert(TicketFactory.ZeroAddress.selector);
        new TicketFactory(address(0), address(escrow), address(resale), address(renderer), cfg, minter);
        vm.expectRevert(TicketFactory.ZeroAddress.selector);
        new TicketFactory(address(implementation), address(0), address(resale), address(renderer), cfg, minter);
        vm.expectRevert(TicketFactory.ZeroAddress.selector);
        new TicketFactory(address(implementation), address(escrow), address(0), address(renderer), cfg, minter);
        vm.expectRevert(TicketFactory.ZeroAddress.selector);
        new TicketFactory(address(implementation), address(escrow), address(resale), address(0), cfg, minter);
    }

    function test_Constructor_WithoutEventCreator() public {
        TicketFactory f = new TicketFactory(
            address(implementation), address(escrow), address(resale), address(renderer), _platformConfig(), address(0)
        );
        assertFalse(f.hasRole(f.EVENT_CREATOR_ROLE(), minter));
        assertEq(f.defaultAdmin(), admin);
    }

    function test_CreateEvent_DeploysWiredCloneAtPredictedAddress() public {
        bytes32 eventId = keccak256("event-2");
        address predicted = factory.predictTicketAddress(eventId);

        vm.expectEmit(true, true, true, true, address(factory));
        emit EventCreated(eventId, predicted, organizer, eventStart, eventEnd);
        EventTicket created = _createEvent(eventId, "Second Event");

        assertEq(address(created), predicted);
        assertEq(factory.ticketOf(eventId), predicted);
        assertEq(factory.eventCount(), 2);
        assertEq(created.name(), "Second Event");
        assertEq(created.escrow(), address(escrow));
        assertEq(created.resaleContract(), address(resale));
        assertTrue(created.hasRole(created.MINTER_ROLE(), minter));
        assertTrue(resale.isRegisteredTicket(predicted));
        (bool registered,,,) = escrow.eventAccount(predicted);
        assertTrue(registered);

        // The clone is a 45-byte EIP-1167 proxy, not a copy of the implementation.
        assertEq(predicted.code.length, 45);
    }

    function test_CreateEvent_RevertWhen_NotCreator() public {
        TicketFactory.CreateEventParams memory p = _createParams(keccak256("event-2"), "x");
        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, factory.EVENT_CREATOR_ROLE()
            )
        );
        vm.prank(stranger);
        factory.createEvent(p);
    }

    function test_CreateEvent_RevertWhen_EventIdReused() public {
        TicketFactory.CreateEventParams memory p = _createParams(EVENT_ID, "Duplicate");
        vm.expectRevert();
        vm.prank(minter);
        factory.createEvent(p);
    }

    function test_SetPlatformConfig() public {
        TicketFactory.PlatformConfig memory cfg = _platformConfig();
        cfg.minter = bob;

        vm.expectRevert(
            abi.encodeWithSelector(
                IAccessControl.AccessControlUnauthorizedAccount.selector, stranger, factory.DEFAULT_ADMIN_ROLE()
            )
        );
        vm.prank(stranger);
        factory.setPlatformConfig(cfg);

        vm.prank(admin);
        factory.setPlatformConfig(cfg);
        EventTicket created = _createEvent(keccak256("event-3"), "Third");
        assertTrue(created.hasRole(created.MINTER_ROLE(), bob));

        cfg.admin = address(0);
        vm.expectRevert(TicketFactory.ZeroAddress.selector);
        vm.prank(admin);
        factory.setPlatformConfig(cfg);
    }

    /// @notice Compares the gas of creating an event as a minimal proxy with deploying the full bytecode.
    function test_Gas_CloneVersusFullDeployment() public {
        TicketFactory.CreateEventParams memory p = _createParams(keccak256("gas-event"), "Gas Event");

        vm.prank(minter);
        uint256 before = gasleft();
        factory.createEvent(p);
        uint256 cloneGas = before - gasleft();

        // Deployed with a raw CREATE so the measurement is the real cost of putting the bytecode on-chain.
        bytes memory creationCode = type(EventTicket).creationCode;
        address fullDeployment;
        before = gasleft();
        assembly ("memory-safe") {
            fullDeployment := create(0, add(creationCode, 0x20), mload(creationCode))
        }
        uint256 implementationGas = before - gasleft();
        assertTrue(fullDeployment != address(0));

        // A non-clone design pays the bytecode deployment for every event on top of initialisation.
        uint256 fullDeployGas = implementationGas + cloneGas;
        console2.log("createEvent via EIP-1167 clone (gas):", cloneGas);
        console2.log("full EventTicket deployment (gas):   ", implementationGas);
        console2.log("estimated non-clone event cost (gas):", fullDeployGas);
        console2.log("saving per event (%):                ", (implementationGas * 100) / fullDeployGas);

        assertLt(cloneGas, implementationGas, "clone should be cheaper than deploying the bytecode");
    }
}
