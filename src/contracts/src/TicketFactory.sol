// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {AccessControlDefaultAdminRules} from
    "@openzeppelin/contracts/access/extensions/AccessControlDefaultAdminRules.sol";
import {Clones} from "@openzeppelin/contracts/proxy/Clones.sol";

import {IEventTicket} from "./interfaces/IEventTicket.sol";
import {IEventEscrow} from "./interfaces/IEventEscrow.sol";
import {IResaleRegistrar} from "./interfaces/IResaleRegistrar.sol";
import {TierInit, TicketInit} from "./TixFlowTypes.sol";

/// @title TicketFactory
/// @notice Deploys one {EventTicket} per event as an EIP-1167 minimal proxy and wires it to the shared
///         escrow and marketplace.
/// @dev Clones are deterministic (CREATE2 with the event ID as salt), so an event's contract address is
///      known before the transaction is mined and the same event can never be deployed twice.
contract TicketFactory is AccessControlDefaultAdminRules {
    /// @notice May create events (the backend, on behalf of approved organizers).
    bytes32 public constant EVENT_CREATOR_ROLE = keccak256("EVENT_CREATOR_ROLE");

    /// @notice Platform-level role holders applied to every new event.
    struct PlatformConfig {
        address admin;
        uint48 adminDelay;
        address minter;
        address redeemer;
        address pauser;
        address voucherSigner;
    }

    struct CreateEventParams {
        bytes32 eventId;
        string name;
        string symbol;
        address organizer;
        uint64 eventStart;
        uint64 eventEnd;
        uint64 unlockTime;
        uint96 royaltyBps;
        TierInit[] tiers;
    }

    /// @notice {EventTicket} implementation every clone delegates to.
    address public immutable implementation;
    /// @notice Escrow holding primary-sale payments.
    address public immutable escrow;
    /// @notice Price-capped marketplace allowed to move locked tickets.
    address public immutable resale;
    /// @notice On-chain metadata renderer.
    address public immutable renderer;

    PlatformConfig public platform;

    mapping(bytes32 eventId => address ticket) public ticketOf;
    uint256 public eventCount;

    event EventCreated(
        bytes32 indexed eventId, address indexed ticket, address indexed organizer, uint64 eventStart, uint64 eventEnd
    );
    event PlatformConfigSet(PlatformConfig config);

    error ZeroAddress();

    /// @param implementation_ Deployed {EventTicket} implementation.
    /// @param escrow_ Primary-sale escrow.
    /// @param resale_ Price-capped marketplace.
    /// @param renderer_ Metadata renderer.
    /// @param config Role holders applied to new events; `config.admin` also administers this factory.
    /// @param eventCreator Initial holder of {EVENT_CREATOR_ROLE}; zero to grant it later.
    constructor(
        address implementation_,
        address escrow_,
        address resale_,
        address renderer_,
        PlatformConfig memory config,
        address eventCreator
    ) AccessControlDefaultAdminRules(config.adminDelay, config.admin) {
        if (eventCreator != address(0)) _grantRole(EVENT_CREATOR_ROLE, eventCreator);
        if (
            implementation_ == address(0) || escrow_ == address(0) || resale_ == address(0)
                || renderer_ == address(0)
        ) revert ZeroAddress();
        implementation = implementation_;
        escrow = escrow_;
        resale = resale_;
        renderer = renderer_;
        platform = config;
        emit PlatformConfigSet(config);
    }

    /// @notice Deploys and initialises the ticket contract for an event.
    /// @dev Reverts if `p.eventId` was already used, because the CREATE2 address is taken.
    /// @param p Event identity, organizer, schedule, royalty and tier configuration.
    /// @return ticket Address of the new ticket contract.
    function createEvent(CreateEventParams calldata p) external onlyRole(EVENT_CREATOR_ROLE) returns (address ticket) {
        ticket = Clones.cloneDeterministic(implementation, p.eventId);
        ticketOf[p.eventId] = ticket;
        unchecked {
            ++eventCount;
        }
        emit EventCreated(p.eventId, ticket, p.organizer, p.eventStart, p.eventEnd);

        PlatformConfig memory config = platform;
        IEventTicket(ticket).initialize(
            TicketInit({
                name: p.name,
                symbol: p.symbol,
                admin: config.admin,
                adminDelay: config.adminDelay,
                organizer: p.organizer,
                minter: config.minter,
                redeemer: config.redeemer,
                pauser: config.pauser,
                voucherSigner: config.voucherSigner,
                resaleContract: resale,
                escrow: escrow,
                renderer: renderer,
                eventStart: p.eventStart,
                eventEnd: p.eventEnd,
                unlockTime: p.unlockTime,
                royaltyBps: p.royaltyBps,
                tiers: p.tiers
            })
        );
        IEventEscrow(escrow).registerEvent(ticket);
        IResaleRegistrar(resale).registerTicket(ticket);
    }

    /// @notice Updates the role holders applied to events created from now on.
    /// @param config New platform configuration.
    function setPlatformConfig(PlatformConfig calldata config) external onlyRole(DEFAULT_ADMIN_ROLE) {
        if (config.admin == address(0)) revert ZeroAddress();
        platform = config;
        emit PlatformConfigSet(config);
    }

    /// @notice Address the ticket contract for `eventId` has, or will have once created.
    /// @param eventId Event identifier used as the CREATE2 salt.
    function predictTicketAddress(bytes32 eventId) external view returns (address) {
        return Clones.predictDeterministicAddress(implementation, eventId, address(this));
    }
}
