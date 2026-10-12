// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Script, console2} from "forge-std/Script.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";

import {EventTicket} from "../src/EventTicket.sol";
import {TicketRenderer} from "../src/TicketRenderer.sol";
import {TicketFactory} from "../src/TicketFactory.sol";
import {EventEscrow} from "../src/EventEscrow.sol";
import {PriceCappedResale} from "../src/PriceCappedResale.sol";
import {SignatureValidator} from "../src/SignatureValidator.sol";
import {IEventEscrow} from "../src/interfaces/IEventEscrow.sol";
import {MockUSDC} from "../src/mocks/MockUSDC.sol";

/// @notice Deploys the whole TixFlow protocol and writes the addresses to `deployments/<name>.json`.
/// @dev Environment:
///   PRIVATE_KEY               deployer key (required)
///   ADMIN_ADDRESS             default admin of every contract          (default: deployer)
///   MINTER_ADDRESS            backend relayer: MINTER, EVENT_CREATOR, CANCELLER (default: deployer)
///   REDEEMER_ADDRESS          redemption checkpoint writer             (default: minter)
///   PAUSER_ADDRESS            emergency pauser                         (default: admin)
///   VOUCHER_SIGNER_ADDRESS    signer of mint vouchers                  (default: minter)
///   ADMIN_DELAY_SECONDS       delay on admin transfers                 (default: 1 day)
///   RESALE_MARKUP_BPS         resale cap over face value               (default: 1000 = 10%)
///   USDC_ADDRESS              payment token; a MockUSDC is deployed on chain 31337 when unset
///   DEPLOYMENT_NAME           output file name                         (default: by chain ID)
contract Deploy is Script {
    address private constant BASE_SEPOLIA_USDC = 0x036CbD53842c5426634e7929541eC2318f3dCF7e;
    address private constant BASE_MAINNET_USDC = 0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913;

    struct Config {
        address deployer;
        address admin;
        address minter;
        address redeemer;
        address pauser;
        address voucherSigner;
        uint48 adminDelay;
        uint16 markupBps;
        address usdc;
        bool usdcIsMock;
    }

    struct Deployed {
        address renderer;
        address implementation;
        address validator;
        address escrow;
        address resale;
        address factory;
    }

    function run() external {
        uint256 deployerKey = vm.envUint("PRIVATE_KEY");
        Config memory cfg = _loadConfig(vm.addr(deployerKey));

        vm.startBroadcast(deployerKey);
        if (cfg.usdc == address(0)) {
            require(block.chainid == 31337, "USDC_ADDRESS required outside the local chain");
            cfg.usdc = address(new MockUSDC());
            cfg.usdcIsMock = true;
        }
        Deployed memory d = _deploy(cfg);
        vm.stopBroadcast();

        _writeJson(cfg, d);
    }

    function _loadConfig(address deployer) private view returns (Config memory cfg) {
        cfg.deployer = deployer;
        cfg.admin = vm.envOr("ADMIN_ADDRESS", deployer);
        cfg.minter = vm.envOr("MINTER_ADDRESS", deployer);
        cfg.redeemer = vm.envOr("REDEEMER_ADDRESS", cfg.minter);
        cfg.pauser = vm.envOr("PAUSER_ADDRESS", cfg.admin);
        cfg.voucherSigner = vm.envOr("VOUCHER_SIGNER_ADDRESS", cfg.minter);
        cfg.adminDelay = uint48(vm.envOr("ADMIN_DELAY_SECONDS", uint256(1 days)));
        cfg.markupBps = uint16(vm.envOr("RESALE_MARKUP_BPS", uint256(1000)));
        cfg.usdc = vm.envOr("USDC_ADDRESS", _defaultUsdc());
    }

    function _deploy(Config memory cfg) private returns (Deployed memory d) {
        d.renderer = address(new TicketRenderer());
        d.implementation = address(new EventTicket());
        d.validator = address(new SignatureValidator());

        // The factory is deployed two transactions after the escrow, so its address is known up front and
        // can be granted the registrar role in the constructors.
        address predictedFactory = vm.computeCreateAddress(cfg.deployer, vm.getNonce(cfg.deployer) + 2);

        d.escrow =
            address(new EventEscrow(IERC20(cfg.usdc), cfg.admin, cfg.adminDelay, predictedFactory, cfg.minter));
        d.resale = address(
            new PriceCappedResale(
                IERC20(cfg.usdc), IEventEscrow(d.escrow), cfg.admin, cfg.adminDelay, cfg.markupBps, predictedFactory
            )
        );
        d.factory = address(
            new TicketFactory(
                d.implementation,
                d.escrow,
                d.resale,
                d.renderer,
                TicketFactory.PlatformConfig({
                    admin: cfg.admin,
                    adminDelay: cfg.adminDelay,
                    minter: cfg.minter,
                    redeemer: cfg.redeemer,
                    pauser: cfg.pauser,
                    voucherSigner: cfg.voucherSigner
                }),
                cfg.minter
            )
        );
        require(d.factory == predictedFactory, "factory address mismatch");
    }

    function _writeJson(Config memory cfg, Deployed memory d) private {
        string memory obj = "deployment";
        vm.serializeUint(obj, "chainId", block.chainid);
        vm.serializeUint(obj, "deployBlock", block.number);
        vm.serializeBool(obj, "usdcIsMock", cfg.usdcIsMock);
        vm.serializeAddress(obj, "usdc", cfg.usdc);
        vm.serializeAddress(obj, "ticketImplementation", d.implementation);
        vm.serializeAddress(obj, "ticketRenderer", d.renderer);
        vm.serializeAddress(obj, "signatureValidator", d.validator);
        vm.serializeAddress(obj, "eventEscrow", d.escrow);
        vm.serializeAddress(obj, "priceCappedResale", d.resale);
        vm.serializeAddress(obj, "admin", cfg.admin);
        vm.serializeAddress(obj, "minter", cfg.minter);
        vm.serializeAddress(obj, "voucherSigner", cfg.voucherSigner);
        string memory json = vm.serializeAddress(obj, "ticketFactory", d.factory);

        string memory path = string.concat("./deployments/", vm.envOr("DEPLOYMENT_NAME", _defaultName()), ".json");
        vm.writeJson(json, path);

        console2.log("Deployment written to", path);
        console2.log("TicketFactory     ", d.factory);
        console2.log("EventEscrow       ", d.escrow);
        console2.log("PriceCappedResale ", d.resale);
        console2.log("USDC              ", cfg.usdc);
    }

    function _defaultUsdc() private view returns (address) {
        if (block.chainid == 84532) return BASE_SEPOLIA_USDC;
        if (block.chainid == 8453) return BASE_MAINNET_USDC;
        return address(0);
    }

    function _defaultName() private view returns (string memory) {
        if (block.chainid == 31337) return "local";
        if (block.chainid == 84532) return "base-sepolia";
        if (block.chainid == 8453) return "base";
        return vm.toString(block.chainid);
    }
}
