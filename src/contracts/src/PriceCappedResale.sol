// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import "openzeppelin-contracts/contracts/utils/ReentrancyGuard.sol";

interface IEventTicket {
    function ownerOf(uint256 tokenId) external view returns (address);
    function safeTransferFrom(address from, address to, uint256 tokenId) external;
}

contract PriceCappedResale is ReentrancyGuard {
    IEventTicket public ticketContract;
    uint256 public constant MAX_MARKUP_BPS = 1000; // 10% = 1000 basis points
    mapping(uint256 => uint256) public faceValue;

    constructor(address _ticketContract) {
        ticketContract = IEventTicket(_ticketContract);
    }

    function listAndSell(uint256 tokenId, address buyer) external payable nonReentrant {
        require(ticketContract.ownerOf(tokenId) == msg.sender, "Not owner");
        uint256 maxPrice = faceValue[tokenId] + (faceValue[tokenId] * MAX_MARKUP_BPS / 10000);
        require(msg.value <= maxPrice, "Exceeds price cap");

        payable(msg.sender).transfer(msg.value);
        ticketContract.safeTransferFrom(msg.sender, buyer, tokenId);
    }
}
