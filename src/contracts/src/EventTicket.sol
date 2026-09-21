// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import "openzeppelin-contracts/contracts/token/ERC721/ERC721.sol";
import "openzeppelin-contracts/contracts/access/Ownable.sol";

contract EventTicket is ERC721, Ownable {
    uint256 private _nextTokenId;
    address public resaleContract;
    mapping(uint256 => bool) public redeemed;

    constructor(string memory name, string memory symbol, address initialOwner)
        ERC721(name, symbol)
        Ownable(initialOwner)
    {}

    function setResaleContract(address _resale) external onlyOwner {
        resaleContract = _resale;
    }

    function mint(address to) external onlyOwner returns (uint256) {
        uint256 tokenId = _nextTokenId++;
        _safeMint(to, tokenId);
        return tokenId;
    }

    function mintBatch(address[] calldata to) external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            uint256 tokenId = _nextTokenId++;
            _safeMint(to[i], tokenId);
        }
    }

    function redeem(uint256 tokenId) external onlyOwner {
        redeemed[tokenId] = true;
    }

    function _update(address to, uint256 tokenId, address auth)
        internal
        override
        returns (address)
    {
        address from = _ownerOf(tokenId);

        if (from != address(0) && to != address(0)) {
            require(
                msg.sender == resaleContract || redeemed[tokenId],
                "EventTicket: transfer restricted"
            );
        }

        return super._update(to, tokenId, auth);
    }
}
