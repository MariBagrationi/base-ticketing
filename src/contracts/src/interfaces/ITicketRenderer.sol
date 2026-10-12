// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {RenderParams} from "../TixFlowTypes.sol";

/// @notice Builds fully on-chain token metadata for a ticket.
interface ITicketRenderer {
    function tokenURI(RenderParams calldata p) external pure returns (string memory);
}
