// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {MintVoucher, TicketInit} from "../TixFlowTypes.sol";

/// @notice Surface of {EventTicket} used by the factory, escrow and marketplace.
interface IEventTicket {
    function initialize(TicketInit calldata p) external;

    function mintWithVoucher(MintVoucher calldata voucher, bytes calldata signature)
        external
        returns (uint256 firstTokenId);

    function ownerOf(uint256 tokenId) external view returns (address);

    function transferFrom(address from, address to, uint256 tokenId) external;

    function organizer() external view returns (address);

    function eventEnd() external view returns (uint64);

    function resaleContract() external view returns (address);

    function tierOf(uint256 tokenId) external view returns (uint256);

    function tierPrice(uint256 tierId) external view returns (uint256);

    function isRedeemed(uint256 tokenId) external view returns (bool);
}
