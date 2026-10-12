// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Base64} from "@openzeppelin/contracts/utils/Base64.sol";
import {Strings} from "@openzeppelin/contracts/utils/Strings.sol";

import {ITicketRenderer} from "./interfaces/ITicketRenderer.sol";
import {RenderParams} from "./TixFlowTypes.sol";

/// @title TicketRenderer
/// @notice Stateless renderer producing fully on-chain ERC-721 metadata (base64 JSON with an embedded SVG).
/// @dev Kept separate from {EventTicket} so the ticket implementation stays under the contract size limit
///      and every event clone shares one copy of the artwork code.
contract TicketRenderer is ITicketRenderer {
    using Strings for uint256;

    /// @notice Builds the `data:application/json;base64,...` token URI for a ticket.
    /// @param p Event name, tier, token ID and redemption state.
    function tokenURI(RenderParams calldata p) external pure returns (string memory) {
        string memory status = p.redeemed ? "Attended" : "Valid";
        bytes memory json = abi.encodePacked(
            '{"name":"',
            _escapeJson(p.eventName),
            " #",
            p.tokenId.toString(),
            '","description":"',
            p.redeemed
                ? "Soulbound proof of attendance. This TixFlow ticket was redeemed at the door."
                : "TixFlow ticket on Base. Transfer-locked and resellable only through the price-capped marketplace.",
            '","image":"data:image/svg+xml;base64,',
            Base64.encode(_svg(p)),
            '","attributes":[{"trait_type":"Tier","value":"',
            _escapeJson(p.tierName),
            '"},{"trait_type":"Status","value":"',
            status,
            '"},{"display_type":"date","trait_type":"Event start","value":',
            uint256(p.eventStart).toString(),
            "}]}"
        );
        return string.concat("data:application/json;base64,", Base64.encode(json));
    }

    function _svg(RenderParams calldata p) private pure returns (bytes memory) {
        return abi.encodePacked(_svgHeader(p.redeemed), _svgBody(p), _svgStub(p.redeemed));
    }

    function _svgHeader(bool redeemed) private pure returns (bytes memory) {
        return abi.encodePacked(
            '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 560" width="400" height="560">',
            '<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="',
            redeemed ? "#B7791F" : "#0052FF",
            '"/><stop offset="1" stop-color="',
            redeemed ? "#3D2A07" : "#001A66",
            '"/></linearGradient></defs>',
            '<rect width="400" height="560" rx="28" fill="url(#g)"/>',
            '<circle cx="0" cy="390" r="18" fill="#0A0D14"/><circle cx="400" cy="390" r="18" fill="#0A0D14"/>',
            '<line x1="30" y1="390" x2="370" y2="390" stroke="#FFF" stroke-opacity=".45" stroke-width="2" stroke-dasharray="8 8"/>',
            '<text x="32" y="58" fill="#FFF" fill-opacity=".75" font-family="monospace" font-size="13" letter-spacing="3">TIXFLOW / BASE</text>'
        );
    }

    function _svgBody(RenderParams calldata p) private pure returns (bytes memory) {
        // Long names are squeezed into the card width instead of overflowing it.
        string memory fit = bytes(p.eventName).length > 24 ? ' textLength="336" lengthAdjust="spacingAndGlyphs"' : "";
        return abi.encodePacked(
            '<text x="32" y="118" fill="#FFF" font-family="sans-serif" font-size="26" font-weight="700"',
            fit,
            ">",
            _escapeXml(p.eventName),
            "</text>",
            '<text x="32" y="188" fill="#FFF" fill-opacity=".6" font-family="monospace" font-size="12" letter-spacing="2">TIER</text>',
            '<text x="32" y="218" fill="#FFF" font-family="sans-serif" font-size="22" font-weight="600">',
            _escapeXml(p.tierName),
            "</text>",
            '<text x="32" y="278" fill="#FFF" fill-opacity=".6" font-family="monospace" font-size="12" letter-spacing="2">TICKET</text>',
            '<text x="32" y="312" fill="#FFF" font-family="monospace" font-size="28" font-weight="700">#',
            p.tokenId.toString(),
            "</text>"
        );
    }

    function _svgStub(bool redeemed) private pure returns (bytes memory) {
        return abi.encodePacked(
            '<rect x="32" y="424" width="',
            redeemed ? "150" : "104",
            '" height="38" rx="19" fill="#FFF" fill-opacity=".16"/>',
            '<text x="52" y="449" fill="#FFF" font-family="sans-serif" font-size="16" font-weight="700">',
            redeemed ? "ATTENDED" : "VALID",
            "</text>",
            '<text x="32" y="512" fill="#FFF" fill-opacity=".7" font-family="monospace" font-size="12" letter-spacing="2">',
            redeemed ? "PROOF OF ATTENDANCE / SOULBOUND" : "ADMIT ONE / TRANSFER-LOCKED",
            "</text></svg>"
        );
    }

    /// @dev Escapes the five XML special characters so a name cannot inject SVG markup.
    function _escapeXml(string calldata input) private pure returns (string memory) {
        bytes calldata raw = bytes(input);
        bytes memory out = "";
        for (uint256 i = 0; i < raw.length; ++i) {
            bytes1 c = raw[i];
            if (c == "&") out = abi.encodePacked(out, "&amp;");
            else if (c == "<") out = abi.encodePacked(out, "&lt;");
            else if (c == ">") out = abi.encodePacked(out, "&gt;");
            else if (c == '"') out = abi.encodePacked(out, "&quot;");
            else if (c == "'") out = abi.encodePacked(out, "&apos;");
            else if (uint8(c) < 0x20) out = abi.encodePacked(out, " ");
            else out = abi.encodePacked(out, c);
        }
        return string(out);
    }

    /// @dev Escapes quotes, backslashes and control characters so a name cannot break out of a JSON string.
    function _escapeJson(string calldata input) private pure returns (string memory) {
        bytes calldata raw = bytes(input);
        bytes memory out = "";
        for (uint256 i = 0; i < raw.length; ++i) {
            bytes1 c = raw[i];
            if (c == '"' || c == "\\") out = abi.encodePacked(out, "\\", c);
            else if (uint8(c) < 0x20) out = abi.encodePacked(out, " ");
            else out = abi.encodePacked(out, c);
        }
        return string(out);
    }
}
