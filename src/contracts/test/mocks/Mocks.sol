// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import {IEventEscrow} from "../../src/interfaces/IEventEscrow.sol";
import {MintVoucher} from "../../src/TixFlowTypes.sol";

/// @notice Minimal ERC-1271 smart wallet controlled by one EOA key.
contract Mock1271Wallet {
    address public immutable owner;

    constructor(address owner_) {
        owner = owner_;
    }

    function isValidSignature(bytes32 hash, bytes calldata signature) external view returns (bytes4) {
        (address recovered, ECDSA.RecoverError err,) = ECDSA.tryRecover(hash, signature);
        if (err == ECDSA.RecoverError.NoError && recovered == owner) return 0x1626ba7e;
        return 0xffffffff;
    }
}

/// @notice ERC-1271 wallet whose signature check always reverts.
contract RevertingWallet {
    function isValidSignature(bytes32, bytes calldata) external pure returns (bytes4) {
        revert("nope");
    }
}

/// @notice CREATE2 factory for {Mock1271Wallet}, used to exercise ERC-6492 counterfactual signatures.
contract MockWalletFactory {
    function deploy(address owner, bytes32 salt) external returns (address) {
        return address(new Mock1271Wallet{salt: salt}(owner));
    }

    function predict(address owner, bytes32 salt) external view returns (address) {
        bytes32 initCodeHash = keccak256(abi.encodePacked(type(Mock1271Wallet).creationCode, abi.encode(owner)));
        return address(uint160(uint256(keccak256(abi.encodePacked(bytes1(0xff), address(this), salt, initCodeHash)))));
    }

    function alwaysReverts() external pure {
        revert("factory failed");
    }
}

/// @notice A registered "ticket" that misbehaves, to prove the escrow's prepaid accounting cannot be fooled.
contract MaliciousTicket {
    enum Mode {
        SkipPayment,
        WrongAmount
    }

    IEventEscrow public immutable escrow;
    Mode public mode;
    uint256 public price = 10e6;

    constructor(IEventEscrow escrow_) {
        escrow = escrow_;
    }

    function setMode(Mode mode_) external {
        mode = mode_;
    }

    function tierPrice(uint256) external view returns (uint256) {
        return price;
    }

    function mintWithVoucher(MintVoucher calldata voucher, bytes calldata) external returns (uint256) {
        if (mode == Mode.WrongAmount) {
            escrow.collectPayment(msg.sender, voucher.buyer, 1, voucher.quantity, price / 2);
        }
        return 1;
    }
}
