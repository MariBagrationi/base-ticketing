// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import "forge-std/Test.sol";
import "../src/EventTicket.sol";

contract EventTicketTest is Test {
    EventTicket ticket;
    address owner = address(this);
    address buyer = address(0x1);
    address scalper = address(0x2);

    function setUp() public {
        ticket = new EventTicket("TixFlow Ticket", "TIX", owner);
    }

    function testMint() public {
        uint256 tokenId = ticket.mint(buyer);
        assertEq(ticket.ownerOf(tokenId), buyer);
    }

    function testTransferRevertsWithoutRedeemOrResale() public {
        uint256 tokenId = ticket.mint(buyer);
        vm.prank(buyer);
        vm.expectRevert("EventTicket: transfer restricted");
        ticket.transferFrom(buyer, scalper, tokenId);
    }

    function testTransferSucceedsAfterRedeem() public {
        uint256 tokenId = ticket.mint(buyer);
        ticket.redeem(tokenId);
        vm.prank(buyer);
        ticket.transferFrom(buyer, scalper, tokenId);
        assertEq(ticket.ownerOf(tokenId), scalper);
    }
}
