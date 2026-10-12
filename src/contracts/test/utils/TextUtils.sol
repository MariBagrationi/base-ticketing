// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

/// @notice Small helpers for asserting on generated metadata in tests.
library TextUtils {
    bytes private constant TABLE = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    function contains(bytes memory haystack, bytes memory needle) internal pure returns (bool) {
        if (needle.length == 0) return true;
        if (needle.length > haystack.length) return false;
        for (uint256 i = 0; i <= haystack.length - needle.length; ++i) {
            bool matched = true;
            for (uint256 j = 0; j < needle.length; ++j) {
                if (haystack[i + j] != needle[j]) {
                    matched = false;
                    break;
                }
            }
            if (matched) return true;
        }
        return false;
    }

    function stripPrefix(string memory value, string memory prefix) internal pure returns (string memory) {
        bytes memory v = bytes(value);
        bytes memory p = bytes(prefix);
        require(v.length >= p.length, "prefix longer than value");
        for (uint256 i = 0; i < p.length; ++i) {
            require(v[i] == p[i], "prefix mismatch");
        }
        bytes memory out = new bytes(v.length - p.length);
        for (uint256 i = 0; i < out.length; ++i) {
            out[i] = v[p.length + i];
        }
        return string(out);
    }

    function decodeBase64(string memory input) internal pure returns (bytes memory) {
        bytes memory data = bytes(input);
        require(data.length % 4 == 0, "invalid base64 length");
        if (data.length == 0) return "";

        uint256 padding = data[data.length - 1] == "=" ? (data[data.length - 2] == "=" ? 2 : 1) : 0;
        bytes memory out = new bytes((data.length / 4) * 3 - padding);

        uint256 outIndex;
        for (uint256 i = 0; i < data.length; i += 4) {
            uint256 chunk = (_value(data[i]) << 18) | (_value(data[i + 1]) << 12) | (_value(data[i + 2]) << 6)
                | _value(data[i + 3]);
            if (outIndex < out.length) out[outIndex++] = bytes1(uint8(chunk >> 16));
            if (outIndex < out.length) out[outIndex++] = bytes1(uint8(chunk >> 8));
            if (outIndex < out.length) out[outIndex++] = bytes1(uint8(chunk));
        }
        return out;
    }

    function _value(bytes1 c) private pure returns (uint256) {
        if (c == "=") return 0;
        for (uint256 i = 0; i < 64; ++i) {
            if (TABLE[i] == c) return i;
        }
        revert("invalid base64 char");
    }
}
