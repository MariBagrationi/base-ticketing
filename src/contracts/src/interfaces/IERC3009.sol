// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

/// @notice The EIP-3009 function USDC exposes for gasless, front-run-safe pulls.
/// @dev `receiveWithAuthorization` requires `msg.sender == to`, so only the payee can submit it.
interface IERC3009 {
    function receiveWithAuthorization(
        address from,
        address to,
        uint256 value,
        uint256 validAfter,
        uint256 validBefore,
        bytes32 nonce,
        uint8 v,
        bytes32 r,
        bytes32 s
    ) external;
}
