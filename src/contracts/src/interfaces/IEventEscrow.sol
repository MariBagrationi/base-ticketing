// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

/// @notice Surface of {EventEscrow} used by ticket contracts, the factory and the marketplace.
interface IEventEscrow {
    function registerEvent(address ticket) external;

    function collectPayment(address payer, address buyer, uint256 firstTokenId, uint256 quantity, uint256 unitPrice)
        external;

    function isCancelled(address ticket) external view returns (bool);
}
