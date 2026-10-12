// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";

import {TextUtils} from "./utils/TextUtils.sol";
import {TicketRenderer} from "../src/TicketRenderer.sol";
import {RenderParams} from "../src/TixFlowTypes.sol";

contract TicketRendererTest is Test {
    TicketRenderer private renderer;

    function setUp() public {
        renderer = new TicketRenderer();
    }

    function test_Render_ValidTicket() public view {
        (bytes memory json, bytes memory svg) = _render("Neon Arches", "VIP Terrace", 7, false);
        assertTrue(TextUtils.contains(json, bytes('"name":"Neon Arches #7"')));
        assertTrue(TextUtils.contains(json, bytes('"display_type":"date","trait_type":"Event start","value":1760000000')));
        assertTrue(TextUtils.contains(svg, bytes(">Neon Arches</text>")));
        assertTrue(TextUtils.contains(svg, bytes(">VIP Terrace</text>")));
        assertTrue(TextUtils.contains(svg, bytes(">#7</text>")));
        assertTrue(TextUtils.contains(svg, bytes(">VALID</text>")));
        assertTrue(TextUtils.contains(svg, bytes("#0052FF")));
        assertFalse(TextUtils.contains(svg, bytes("textLength")));
    }

    function test_Render_RedeemedTicketLooksDifferent() public view {
        (bytes memory json, bytes memory svg) = _render("Neon Arches", "VIP Terrace", 7, true);
        assertTrue(TextUtils.contains(json, bytes('"value":"Attended"')));
        assertTrue(TextUtils.contains(svg, bytes(">ATTENDED</text>")));
        assertTrue(TextUtils.contains(svg, bytes("PROOF OF ATTENDANCE / SOULBOUND")));
        assertTrue(TextUtils.contains(svg, bytes("#B7791F")));
    }

    function test_Render_LongNamesAreSqueezedIntoTheCard() public view {
        (, bytes memory svg) = _render("Symphony of Distortion: Rock Classics Live", "Stalls", 1, false);
        assertTrue(TextUtils.contains(svg, bytes('textLength="336"')));
    }

    /// @notice A hostile event or tier name cannot inject SVG markup or break out of the JSON string.
    function test_Render_EscapesHostileNames() public view {
        string memory hostile = "<script>alert('x')</script> & \"quoted\" \\ back\nline";
        (bytes memory json, bytes memory svg) = _render(hostile, hostile, 1, false);

        assertFalse(TextUtils.contains(svg, bytes("<script>")));
        assertTrue(
            TextUtils.contains(svg, bytes("&lt;script&gt;alert(&apos;x&apos;)&lt;/script&gt; &amp; &quot;quoted&quot;"))
        );
        assertTrue(TextUtils.contains(json, bytes('\\"quoted\\" \\\\ back line')));
        // The raw newline must be gone, otherwise the JSON would be invalid.
        assertFalse(TextUtils.contains(json, bytes("back\nline")));
    }

    function _render(string memory eventName, string memory tierName, uint256 tokenId, bool redeemed)
        private
        view
        returns (bytes memory json, bytes memory svg)
    {
        string memory uri = renderer.tokenURI(
            RenderParams({
                eventName: eventName,
                tierName: tierName,
                tokenId: tokenId,
                tierId: 1,
                eventStart: 1_760_000_000,
                redeemed: redeemed
            })
        );
        json = TextUtils.decodeBase64(TextUtils.stripPrefix(uri, "data:application/json;base64,"));
        svg = _extractSvg(json);
    }

    function _extractSvg(bytes memory json) private pure returns (bytes memory) {
        bytes memory marker = bytes("data:image/svg+xml;base64,");
        uint256 start = _indexOf(json, marker, 0) + marker.length;
        uint256 end = _indexOf(json, bytes('"'), start);
        bytes memory encoded = new bytes(end - start);
        for (uint256 i = 0; i < encoded.length; ++i) {
            encoded[i] = json[start + i];
        }
        return TextUtils.decodeBase64(string(encoded));
    }

    function _indexOf(bytes memory haystack, bytes memory needle, uint256 from) private pure returns (uint256) {
        for (uint256 i = from; i + needle.length <= haystack.length; ++i) {
            bool matched = true;
            for (uint256 j = 0; j < needle.length; ++j) {
                if (haystack[i + j] != needle[j]) {
                    matched = false;
                    break;
                }
            }
            if (matched) return i;
        }
        revert("needle not found");
    }
}
