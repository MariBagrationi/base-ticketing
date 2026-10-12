// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";

import {SignatureValidator} from "../src/SignatureValidator.sol";
import {Mock1271Wallet, MockWalletFactory, RevertingWallet} from "./mocks/Mocks.sol";

contract SignatureValidatorTest is Test {
    bytes32 private constant SUFFIX = 0x6492649264926492649264926492649264926492649264926492649264926492;

    SignatureValidator private validator;
    MockWalletFactory private walletFactory;
    uint256 private ownerKey = uint256(keccak256("wallet-owner"));
    address private owner = vm.addr(ownerKey);
    bytes32 private digest = keccak256("Sign in to TixFlow");

    function setUp() public {
        validator = new SignatureValidator();
        walletFactory = new MockWalletFactory();
    }

    function test_EOA_ValidAndInvalid() public {
        assertTrue(validator.isValidSig(owner, digest, _sign(ownerKey)));
        assertFalse(validator.isValidSig(owner, digest, _sign(ownerKey + 1)));
        assertFalse(validator.isValidSig(owner, keccak256("other"), _sign(ownerKey)));
    }

    function test_EOA_MalformedSignatureIsInvalid() public {
        assertFalse(validator.isValidSig(owner, digest, hex"1234"));
        assertFalse(validator.isValidSig(owner, digest, ""));
    }

    function test_ERC1271_DeployedWallet() public {
        Mock1271Wallet wallet = new Mock1271Wallet(owner);
        assertTrue(validator.isValidSig(address(wallet), digest, _sign(ownerKey)));
        assertFalse(validator.isValidSig(address(wallet), digest, _sign(ownerKey + 1)));
    }

    function test_ERC1271_RevertingWalletIsInvalid() public {
        RevertingWallet wallet = new RevertingWallet();
        assertFalse(validator.isValidSig(address(wallet), digest, _sign(ownerKey)));
    }

    function test_ERC6492_CounterfactualWalletIsDeployedInsideTheCall() public {
        bytes32 salt = keccak256("salt");
        address predicted = walletFactory.predict(owner, salt);
        assertEq(predicted.code.length, 0);

        assertTrue(validator.isValidSig(predicted, digest, _wrap(salt, _sign(ownerKey))));
        assertGt(predicted.code.length, 0);
    }

    function test_ERC6492_WrongOwnerIsInvalid() public {
        bytes32 salt = keccak256("salt");
        address predicted = walletFactory.predict(owner, salt);
        assertFalse(validator.isValidSig(predicted, digest, _wrap(salt, _sign(ownerKey + 1))));
    }

    function test_ERC6492_WrappedSignatureForAlreadyDeployedWallet() public {
        bytes32 salt = keccak256("salt");
        address wallet = walletFactory.deploy(owner, salt);
        // The factory call would revert now (address taken), so the validator must skip it.
        assertTrue(validator.isValidSig(wallet, digest, _wrap(salt, _sign(ownerKey))));
    }

    function test_ERC6492_RevertWhen_FactoryCallFails() public {
        bytes memory wrapped = abi.encodePacked(
            abi.encode(address(walletFactory), abi.encodeCall(MockWalletFactory.alwaysReverts, ()), _sign(ownerKey)),
            SUFFIX
        );
        vm.expectRevert();
        validator.isValidSig(makeAddr("undeployed"), digest, wrapped);
    }

    function _sign(uint256 key) private view returns (bytes memory) {
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(key, digest);
        return abi.encodePacked(r, s, v);
    }

    function _wrap(bytes32 salt, bytes memory inner) private view returns (bytes memory) {
        return abi.encodePacked(
            abi.encode(address(walletFactory), abi.encodeCall(MockWalletFactory.deploy, (owner, salt)), inner), SUFFIX
        );
    }
}
