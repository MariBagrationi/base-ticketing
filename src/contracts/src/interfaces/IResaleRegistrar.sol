// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

/// @notice Registration hook the factory calls on the marketplace for every new event.
interface IResaleRegistrar {
    function registerTicket(address ticket) external;
}
