// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import "forge-std/Script.sol";
import "../src/EventTicket.sol";

contract DeployScript is Script {
    function run() external {
        uint256 deployerKey = vm.envUint("PRIVATE_KEY");
        address deployer = vm.addr(deployerKey);

        vm.startBroadcast(deployerKey);
        EventTicket ticket = new EventTicket("TixFlow Ticket", "TIX", deployer);
        vm.stopBroadcast();

        console.log("EventTicket deployed at:", address(ticket));
    }
}
