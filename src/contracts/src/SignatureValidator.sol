// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import {IERC1271} from "@openzeppelin/contracts/interfaces/IERC1271.sol";

/// @title SignatureValidator
/// @notice Universal signature check for EOAs, deployed smart wallets (ERC-1271) and not-yet-deployed
///         smart wallets (ERC-6492), following the ERC-6492 reference validator.
/// @dev The backend calls {isValidSig} with `eth_call` to verify Sign-In with Ethereum messages from
///      Coinbase Smart Wallet and similar accounts. The call may deploy a counterfactual wallet inside the
///      simulation, which is why the function is not `view`. The contract holds no funds and no roles, so
///      executing it in a real transaction grants a caller nothing.
contract SignatureValidator {
    bytes32 private constant ERC6492_DETECTION_SUFFIX =
        0x6492649264926492649264926492649264926492649264926492649264926492;
    bytes4 private constant ERC1271_MAGIC_VALUE = 0x1626ba7e;

    error WalletDeploymentFailed(bytes reason);

    /// @notice Checks whether `signature` is a valid signature of `hash` by `signer`.
    /// @param signer Account that allegedly signed.
    /// @param hash Digest that was signed.
    /// @param signature EOA signature, ERC-1271 signature, or ERC-6492 wrapped signature.
    /// @return True if the signature is valid for `signer`.
    function isValidSig(address signer, bytes32 hash, bytes calldata signature) external returns (bool) {
        bool wrapped = signature.length >= 32
            && bytes32(signature[signature.length - 32:]) == ERC6492_DETECTION_SUFFIX;
        bool hasCode = signer.code.length != 0;

        if (wrapped) {
            (address walletFactory, bytes memory factoryCalldata, bytes memory innerSignature) =
                abi.decode(signature[:signature.length - 32], (address, bytes, bytes));
            if (!hasCode) {
                // ERC-6492 requires an arbitrary factory call; this contract has nothing a callee could abuse.
                // slither-disable-next-line low-level-calls,missing-zero-check
                (bool ok, bytes memory reason) = walletFactory.call(factoryCalldata);
                if (!ok) revert WalletDeploymentFailed(reason);
            }
            return _isValidERC1271(signer, hash, innerSignature);
        }

        if (hasCode) return _isValidERC1271(signer, hash, signature);

        // slither-disable-next-line unused-return
        (address recovered, ECDSA.RecoverError err,) = ECDSA.tryRecover(hash, signature);
        return err == ECDSA.RecoverError.NoError && recovered == signer;
    }

    function _isValidERC1271(address signer, bytes32 hash, bytes memory signature) private view returns (bool) {
        try IERC1271(signer).isValidSignature(hash, signature) returns (bytes4 magic) {
            return magic == ERC1271_MAGIC_VALUE;
        } catch {
            return false;
        }
    }
}
