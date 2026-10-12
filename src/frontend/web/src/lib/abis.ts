import { parseAbi } from "viem";

// Only the functions the browser calls. Full ABIs live in src/contracts/abi.

export const ticketAbi = parseAbi([
  "function ownerOf(uint256 tokenId) view returns (address)",
  "function isRedeemed(uint256 tokenId) view returns (bool)",
  "function tokenURI(uint256 tokenId) view returns (string)",
  "function totalMinted() view returns (uint256)",
  "function unlockTime() view returns (uint64)",
  "function isTransferable(uint256 tokenId, address operator) view returns (bool)",
  "function nonceUsed(address buyer, uint256 nonce) view returns (bool)",
  "function mintWithVoucher((address buyer, uint256 tierId, uint256 quantity, uint256 nonce, uint256 deadline) voucher, bytes signature) returns (uint256)",
  "function claimAttendanceBadge(uint256 tokenId, bytes32[] proof)",
]);

export const escrowAbi = parseAbi([
  "function paidFor(address ticket, uint256 tokenId) view returns (uint256)",
  "function eventAccount(address ticket) view returns (bool registered, bool cancelled, uint256 held, uint256 released)",
  "function purchaseWithAuthorization(address ticket, (address buyer, uint256 tierId, uint256 quantity, uint256 nonce, uint256 deadline) voucher, bytes voucherSignature, (uint256 validAfter, uint256 validBefore, bytes32 nonce, uint8 v, bytes32 r, bytes32 s) auth) returns (uint256)",
  "function claimRefund(address ticket, uint256 tokenId)",
]);

export const usdcAbi = parseAbi([
  "function name() view returns (string)",
  "function version() view returns (string)",
  "function balanceOf(address account) view returns (uint256)",
]);
