// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {ERC20} from "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import {EIP712} from "@openzeppelin/contracts/utils/cryptography/EIP712.sol";
import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";

/// @title MockUSDC
/// @notice Local and test stand-in for Circle's USDC (FiatTokenV2): 6 decimals, EIP-2612 permit and
///         EIP-3009 transfer/receive with authorisation, with the same EIP-712 domain layout
///         (`name`, version "2").
/// @dev NOT FOR PRODUCTION: `mint` is an open faucet.
contract MockUSDC is ERC20, EIP712 {
    bytes32 public constant PERMIT_TYPEHASH =
        keccak256("Permit(address owner,address spender,uint256 value,uint256 nonce,uint256 deadline)");
    bytes32 public constant TRANSFER_WITH_AUTHORIZATION_TYPEHASH = keccak256(
        "TransferWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)"
    );
    bytes32 public constant RECEIVE_WITH_AUTHORIZATION_TYPEHASH = keccak256(
        "ReceiveWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)"
    );

    /// @notice Largest amount one faucet call can mint (10,000 USDC).
    uint256 public constant FAUCET_LIMIT = 10_000e6;

    mapping(address owner => uint256) public nonces;
    mapping(address authorizer => mapping(bytes32 nonce => bool)) public authorizationState;

    event AuthorizationUsed(address indexed authorizer, bytes32 indexed nonce);

    error FaucetLimitExceeded();
    error PermitExpired();
    error InvalidSignature();
    error AuthorizationNotYetValid();
    error AuthorizationExpired();
    error AuthorizationAlreadyUsed();
    error CallerMustBePayee();

    constructor() ERC20("USD Coin", "USDC") EIP712("USD Coin", "2") {}

    /// @notice USDC uses 6 decimals.
    function decimals() public pure override returns (uint8) {
        return 6;
    }

    /// @notice EIP-712 domain version, as exposed by FiatTokenV2.
    function version() external pure returns (string memory) {
        return "2";
    }

    /// @notice EIP-712 domain separator.
    function DOMAIN_SEPARATOR() external view returns (bytes32) {
        return _domainSeparatorV4();
    }

    /// @notice Open faucet for local chains and tests.
    /// @param to Recipient.
    /// @param amount Amount to mint, at most {FAUCET_LIMIT}.
    function mint(address to, uint256 amount) external {
        if (amount > FAUCET_LIMIT) revert FaucetLimitExceeded();
        _mint(to, amount);
    }

    /// @notice EIP-2612 permit.
    function permit(address owner, address spender, uint256 value, uint256 deadline, uint8 v, bytes32 r, bytes32 s)
        external
    {
        if (block.timestamp > deadline) revert PermitExpired();
        bytes32 structHash = keccak256(abi.encode(PERMIT_TYPEHASH, owner, spender, value, nonces[owner]++, deadline));
        if (ECDSA.recover(_hashTypedDataV4(structHash), v, r, s) != owner) revert InvalidSignature();
        _approve(owner, spender, value);
    }

    /// @notice EIP-3009 transfer submitted by anyone on behalf of `from`.
    function transferWithAuthorization(
        address from,
        address to,
        uint256 value,
        uint256 validAfter,
        uint256 validBefore,
        bytes32 nonce,
        uint8 v,
        bytes32 r,
        bytes32 s
    ) external {
        _useAuthorization(TRANSFER_WITH_AUTHORIZATION_TYPEHASH, from, to, value, validAfter, validBefore, nonce, v, r, s);
        _transfer(from, to, value);
    }

    /// @notice EIP-3009 transfer that only the payee may submit, which prevents front-running.
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
    ) external {
        if (to != msg.sender) revert CallerMustBePayee();
        _useAuthorization(RECEIVE_WITH_AUTHORIZATION_TYPEHASH, from, to, value, validAfter, validBefore, nonce, v, r, s);
        _transfer(from, to, value);
    }

    function _useAuthorization(
        bytes32 typeHash,
        address from,
        address to,
        uint256 value,
        uint256 validAfter,
        uint256 validBefore,
        bytes32 nonce,
        uint8 v,
        bytes32 r,
        bytes32 s
    ) private {
        if (block.timestamp <= validAfter) revert AuthorizationNotYetValid();
        if (block.timestamp >= validBefore) revert AuthorizationExpired();
        if (authorizationState[from][nonce]) revert AuthorizationAlreadyUsed();

        bytes32 structHash = keccak256(abi.encode(typeHash, from, to, value, validAfter, validBefore, nonce));
        if (ECDSA.recover(_hashTypedDataV4(structHash), v, r, s) != from) revert InvalidSignature();

        authorizationState[from][nonce] = true;
        emit AuthorizationUsed(from, nonce);
    }
}
