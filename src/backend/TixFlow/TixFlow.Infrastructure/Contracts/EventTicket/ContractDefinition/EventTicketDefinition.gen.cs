using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts;
using System.Threading;

namespace TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition
{


    public partial class EventTicketDeployment : EventTicketDeploymentBase
    {
        public EventTicketDeployment() : base(BYTECODE) { }
        public EventTicketDeployment(string byteCode) : base(byteCode) { }
    }

    public class EventTicketDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x";
        public EventTicketDeploymentBase() : base(BYTECODE) { }
        public EventTicketDeploymentBase(string byteCode) : base(byteCode) { }

    }

    public partial class DefaultAdminRoleFunction : DefaultAdminRoleFunctionBase { }

    [Function("DEFAULT_ADMIN_ROLE", "bytes32")]
    public class DefaultAdminRoleFunctionBase : FunctionMessage
    {

    }

    public partial class MinterRoleFunction : MinterRoleFunctionBase { }

    [Function("MINTER_ROLE", "bytes32")]
    public class MinterRoleFunctionBase : FunctionMessage
    {

    }

    public partial class MintVoucherTypehashFunction : MintVoucherTypehashFunctionBase { }

    [Function("MINT_VOUCHER_TYPEHASH", "bytes32")]
    public class MintVoucherTypehashFunctionBase : FunctionMessage
    {

    }

    public partial class PauserRoleFunction : PauserRoleFunctionBase { }

    [Function("PAUSER_ROLE", "bytes32")]
    public class PauserRoleFunctionBase : FunctionMessage
    {

    }

    public partial class RedeemerRoleFunction : RedeemerRoleFunctionBase { }

    [Function("REDEEMER_ROLE", "bytes32")]
    public class RedeemerRoleFunctionBase : FunctionMessage
    {

    }

    public partial class AcceptDefaultAdminTransferFunction : AcceptDefaultAdminTransferFunctionBase { }

    [Function("acceptDefaultAdminTransfer")]
    public class AcceptDefaultAdminTransferFunctionBase : FunctionMessage
    {

    }

    public partial class ApproveFunction : ApproveFunctionBase { }

    [Function("approve")]
    public class ApproveFunctionBase : FunctionMessage
    {
        [Parameter("address", "to", 1)]
        public virtual string To { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class BadgesClaimedFunction : BadgesClaimedFunctionBase { }

    [Function("badgesClaimed", "uint256")]
    public class BadgesClaimedFunctionBase : FunctionMessage
    {

    }

    public partial class BalanceOfFunction : BalanceOfFunctionBase { }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunctionBase : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
    }

    public partial class BeginDefaultAdminTransferFunction : BeginDefaultAdminTransferFunctionBase { }

    [Function("beginDefaultAdminTransfer")]
    public class BeginDefaultAdminTransferFunctionBase : FunctionMessage
    {
        [Parameter("address", "newAdmin", 1)]
        public virtual string NewAdmin { get; set; }
    }

    public partial class CancelDefaultAdminTransferFunction : CancelDefaultAdminTransferFunctionBase { }

    [Function("cancelDefaultAdminTransfer")]
    public class CancelDefaultAdminTransferFunctionBase : FunctionMessage
    {

    }

    public partial class ChangeDefaultAdminDelayFunction : ChangeDefaultAdminDelayFunctionBase { }

    [Function("changeDefaultAdminDelay")]
    public class ChangeDefaultAdminDelayFunctionBase : FunctionMessage
    {
        [Parameter("uint48", "newDelay", 1)]
        public virtual ulong NewDelay { get; set; }
    }

    public partial class CheckpointCountFunction : CheckpointCountFunctionBase { }

    [Function("checkpointCount", "uint256")]
    public class CheckpointCountFunctionBase : FunctionMessage
    {

    }

    public partial class CheckpointRedemptionsFunction : CheckpointRedemptionsFunctionBase { }

    [Function("checkpointRedemptions")]
    public class CheckpointRedemptionsFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "root", 1)]
        public virtual byte[] Root { get; set; }
        [Parameter("uint256", "count", 2)]
        public virtual BigInteger Count { get; set; }
    }

    public partial class ClaimAttendanceBadgeFunction : ClaimAttendanceBadgeFunctionBase { }

    [Function("claimAttendanceBadge")]
    public class ClaimAttendanceBadgeFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("bytes32[]", "proof", 2)]
        public virtual List<byte[]> Proof { get; set; }
    }

    public partial class ConfigureTierFunction : ConfigureTierFunctionBase { }

    [Function("configureTier")]
    public class ConfigureTierFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
        [Parameter("string", "tierName", 2)]
        public virtual string TierName { get; set; }
        [Parameter("uint256", "price", 3)]
        public virtual BigInteger Price { get; set; }
        [Parameter("uint256", "supplyCap", 4)]
        public virtual BigInteger SupplyCap { get; set; }
        [Parameter("uint256", "maxPerWallet", 5)]
        public virtual BigInteger MaxPerWallet { get; set; }
    }

    public partial class DefaultAdminFunction : DefaultAdminFunctionBase { }

    [Function("defaultAdmin", "address")]
    public class DefaultAdminFunctionBase : FunctionMessage
    {

    }

    public partial class DefaultAdminDelayFunction : DefaultAdminDelayFunctionBase { }

    [Function("defaultAdminDelay", "uint48")]
    public class DefaultAdminDelayFunctionBase : FunctionMessage
    {

    }

    public partial class DefaultAdminDelayIncreaseWaitFunction : DefaultAdminDelayIncreaseWaitFunctionBase { }

    [Function("defaultAdminDelayIncreaseWait", "uint48")]
    public class DefaultAdminDelayIncreaseWaitFunctionBase : FunctionMessage
    {

    }

    public partial class Eip712DomainFunction : Eip712DomainFunctionBase { }

    [Function("eip712Domain", typeof(Eip712DomainOutputDTO))]
    public class Eip712DomainFunctionBase : FunctionMessage
    {

    }

    public partial class EscrowFunction : EscrowFunctionBase { }

    [Function("escrow", "address")]
    public class EscrowFunctionBase : FunctionMessage
    {

    }

    public partial class EventEndFunction : EventEndFunctionBase { }

    [Function("eventEnd", "uint64")]
    public class EventEndFunctionBase : FunctionMessage
    {

    }

    public partial class EventStartFunction : EventStartFunctionBase { }

    [Function("eventStart", "uint64")]
    public class EventStartFunctionBase : FunctionMessage
    {

    }

    public partial class GetApprovedFunction : GetApprovedFunctionBase { }

    [Function("getApproved", "address")]
    public class GetApprovedFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class GetRoleAdminFunction : GetRoleAdminFunctionBase { }

    [Function("getRoleAdmin", "bytes32")]
    public class GetRoleAdminFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public virtual byte[] Role { get; set; }
    }

    public partial class GrantRoleFunction : GrantRoleFunctionBase { }

    [Function("grantRole")]
    public class GrantRoleFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public virtual byte[] Role { get; set; }
        [Parameter("address", "account", 2)]
        public virtual string Account { get; set; }
    }

    public partial class HasRoleFunction : HasRoleFunctionBase { }

    [Function("hasRole", "bool")]
    public class HasRoleFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public virtual byte[] Role { get; set; }
        [Parameter("address", "account", 2)]
        public virtual string Account { get; set; }
    }

    public partial class HashVoucherFunction : HashVoucherFunctionBase { }

    [Function("hashVoucher", "bytes32")]
    public class HashVoucherFunctionBase : FunctionMessage
    {
        [Parameter("tuple", "voucher", 1)]
        public virtual MintVoucher Voucher { get; set; }
    }

    public partial class InitializeFunction : InitializeFunctionBase { }

    [Function("initialize")]
    public class InitializeFunctionBase : FunctionMessage
    {
        [Parameter("tuple", "p", 1)]
        public virtual TicketInit P { get; set; }
    }

    public partial class IsApprovedForAllFunction : IsApprovedForAllFunctionBase { }

    [Function("isApprovedForAll", "bool")]
    public class IsApprovedForAllFunctionBase : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
        [Parameter("address", "operator", 2)]
        public virtual string Operator { get; set; }
    }

    public partial class IsRedeemedFunction : IsRedeemedFunctionBase { }

    [Function("isRedeemed", "bool")]
    public class IsRedeemedFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class IsTransferableFunction : IsTransferableFunctionBase { }

    [Function("isTransferable", "bool")]
    public class IsTransferableFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("address", "operator", 2)]
        public virtual string Operator { get; set; }
    }

    public partial class MintBatchFunction : MintBatchFunctionBase { }

    [Function("mintBatch")]
    public class MintBatchFunctionBase : FunctionMessage
    {
        [Parameter("tuple[]", "orders", 1)]
        public virtual List<MintOrder> Orders { get; set; }
    }

    public partial class MintWithVoucherFunction : MintWithVoucherFunctionBase { }

    [Function("mintWithVoucher", "uint256")]
    public class MintWithVoucherFunctionBase : FunctionMessage
    {
        [Parameter("tuple", "voucher", 1)]
        public virtual MintVoucher Voucher { get; set; }
        [Parameter("bytes", "signature", 2)]
        public virtual byte[] Signature { get; set; }
    }

    public partial class MintedByFunction : MintedByFunctionBase { }

    [Function("mintedBy", "uint256")]
    public class MintedByFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
        [Parameter("address", "wallet", 2)]
        public virtual string Wallet { get; set; }
    }

    public partial class NameFunction : NameFunctionBase { }

    [Function("name", "string")]
    public class NameFunctionBase : FunctionMessage
    {

    }

    public partial class NonceUsedFunction : NonceUsedFunctionBase { }

    [Function("nonceUsed", "bool")]
    public class NonceUsedFunctionBase : FunctionMessage
    {
        [Parameter("address", "buyer", 1)]
        public virtual string Buyer { get; set; }
        [Parameter("uint256", "nonce", 2)]
        public virtual BigInteger Nonce { get; set; }
    }

    public partial class OrganizerFunction : OrganizerFunctionBase { }

    [Function("organizer", "address")]
    public class OrganizerFunctionBase : FunctionMessage
    {

    }

    public partial class OwnerFunction : OwnerFunctionBase { }

    [Function("owner", "address")]
    public class OwnerFunctionBase : FunctionMessage
    {

    }

    public partial class OwnerOfFunction : OwnerOfFunctionBase { }

    [Function("ownerOf", "address")]
    public class OwnerOfFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class PauseFunction : PauseFunctionBase { }

    [Function("pause")]
    public class PauseFunctionBase : FunctionMessage
    {

    }

    public partial class PausedFunction : PausedFunctionBase { }

    [Function("paused", "bool")]
    public class PausedFunctionBase : FunctionMessage
    {

    }

    public partial class PendingDefaultAdminFunction : PendingDefaultAdminFunctionBase { }

    [Function("pendingDefaultAdmin", typeof(PendingDefaultAdminOutputDTO))]
    public class PendingDefaultAdminFunctionBase : FunctionMessage
    {

    }

    public partial class PendingDefaultAdminDelayFunction : PendingDefaultAdminDelayFunctionBase { }

    [Function("pendingDefaultAdminDelay", typeof(PendingDefaultAdminDelayOutputDTO))]
    public class PendingDefaultAdminDelayFunctionBase : FunctionMessage
    {

    }

    public partial class RedemptionCountFunction : RedemptionCountFunctionBase { }

    [Function("redemptionCount", "uint256")]
    public class RedemptionCountFunctionBase : FunctionMessage
    {

    }

    public partial class RedemptionRootFunction : RedemptionRootFunctionBase { }

    [Function("redemptionRoot", "bytes32")]
    public class RedemptionRootFunctionBase : FunctionMessage
    {

    }

    public partial class RendererFunction : RendererFunctionBase { }

    [Function("renderer", "address")]
    public class RendererFunctionBase : FunctionMessage
    {

    }

    public partial class RenounceRoleFunction : RenounceRoleFunctionBase { }

    [Function("renounceRole")]
    public class RenounceRoleFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public virtual byte[] Role { get; set; }
        [Parameter("address", "account", 2)]
        public virtual string Account { get; set; }
    }

    public partial class ResaleContractFunction : ResaleContractFunctionBase { }

    [Function("resaleContract", "address")]
    public class ResaleContractFunctionBase : FunctionMessage
    {

    }

    public partial class RevokeRoleFunction : RevokeRoleFunctionBase { }

    [Function("revokeRole")]
    public class RevokeRoleFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public virtual byte[] Role { get; set; }
        [Parameter("address", "account", 2)]
        public virtual string Account { get; set; }
    }

    public partial class RollbackDefaultAdminDelayFunction : RollbackDefaultAdminDelayFunctionBase { }

    [Function("rollbackDefaultAdminDelay")]
    public class RollbackDefaultAdminDelayFunctionBase : FunctionMessage
    {

    }

    public partial class RoyaltyInfoFunction : RoyaltyInfoFunctionBase { }

    [Function("royaltyInfo", typeof(RoyaltyInfoOutputDTO))]
    public class RoyaltyInfoFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("uint256", "salePrice", 2)]
        public virtual BigInteger SalePrice { get; set; }
    }

    public partial class SafeTransferFromFunction : SafeTransferFromFunctionBase { }

    [Function("safeTransferFrom")]
    public class SafeTransferFromFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "tokenId", 3)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class SafeTransferFrom1Function : SafeTransferFrom1FunctionBase { }

    [Function("safeTransferFrom")]
    public class SafeTransferFrom1FunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "tokenId", 3)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("bytes", "data", 4)]
        public virtual byte[] Data { get; set; }
    }

    public partial class SetApprovalForAllFunction : SetApprovalForAllFunctionBase { }

    [Function("setApprovalForAll")]
    public class SetApprovalForAllFunctionBase : FunctionMessage
    {
        [Parameter("address", "operator", 1)]
        public virtual string Operator { get; set; }
        [Parameter("bool", "approved", 2)]
        public virtual bool Approved { get; set; }
    }

    public partial class SetResaleContractFunction : SetResaleContractFunctionBase { }

    [Function("setResaleContract")]
    public class SetResaleContractFunctionBase : FunctionMessage
    {
        [Parameter("address", "newResaleContract", 1)]
        public virtual string NewResaleContract { get; set; }
    }

    public partial class SetUnlockTimeFunction : SetUnlockTimeFunctionBase { }

    [Function("setUnlockTime")]
    public class SetUnlockTimeFunctionBase : FunctionMessage
    {
        [Parameter("uint64", "newUnlockTime", 1)]
        public virtual ulong NewUnlockTime { get; set; }
    }

    public partial class SetVoucherSignerFunction : SetVoucherSignerFunctionBase { }

    [Function("setVoucherSigner")]
    public class SetVoucherSignerFunctionBase : FunctionMessage
    {
        [Parameter("address", "signer", 1)]
        public virtual string Signer { get; set; }
    }

    public partial class SupportsInterfaceFunction : SupportsInterfaceFunctionBase { }

    [Function("supportsInterface", "bool")]
    public class SupportsInterfaceFunctionBase : FunctionMessage
    {
        [Parameter("bytes4", "interfaceId", 1)]
        public virtual byte[] InterfaceId { get; set; }
    }

    public partial class SymbolFunction : SymbolFunctionBase { }

    [Function("symbol", "string")]
    public class SymbolFunctionBase : FunctionMessage
    {

    }

    public partial class TierConfigFunction : TierConfigFunctionBase { }

    [Function("tierConfig", typeof(TierConfigOutputDTO))]
    public class TierConfigFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
    }

    public partial class TierOfFunction : TierOfFunctionBase { }

    [Function("tierOf", "uint256")]
    public class TierOfFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class TierPriceFunction : TierPriceFunctionBase { }

    [Function("tierPrice", "uint256")]
    public class TierPriceFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
    }

    public partial class TokenURIFunction : TokenURIFunctionBase { }

    [Function("tokenURI", "string")]
    public class TokenURIFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class TotalMintedFunction : TotalMintedFunctionBase { }

    [Function("totalMinted", "uint256")]
    public class TotalMintedFunctionBase : FunctionMessage
    {

    }

    public partial class TransferFromFunction : TransferFromFunctionBase { }

    [Function("transferFrom")]
    public class TransferFromFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "tokenId", 3)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class UnlockTimeFunction : UnlockTimeFunctionBase { }

    [Function("unlockTime", "uint64")]
    public class UnlockTimeFunctionBase : FunctionMessage
    {

    }

    public partial class UnpauseFunction : UnpauseFunctionBase { }

    [Function("unpause")]
    public class UnpauseFunctionBase : FunctionMessage
    {

    }

    public partial class VoucherSignerFunction : VoucherSignerFunctionBase { }

    [Function("voucherSigner", "address")]
    public class VoucherSignerFunctionBase : FunctionMessage
    {

    }

    public partial class ApprovalEventDTO : ApprovalEventDTOBase { }

    [Event("Approval")]
    public class ApprovalEventDTOBase : IEventDTO
    {
        [Parameter("address", "owner", 1, true )]
        public virtual string Owner { get; set; }
        [Parameter("address", "approved", 2, true )]
        public virtual string Approved { get; set; }
        [Parameter("uint256", "tokenId", 3, true )]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class ApprovalForAllEventDTO : ApprovalForAllEventDTOBase { }

    [Event("ApprovalForAll")]
    public class ApprovalForAllEventDTOBase : IEventDTO
    {
        [Parameter("address", "owner", 1, true )]
        public virtual string Owner { get; set; }
        [Parameter("address", "operator", 2, true )]
        public virtual string Operator { get; set; }
        [Parameter("bool", "approved", 3, false )]
        public virtual bool Approved { get; set; }
    }

    public partial class DefaultAdminDelayChangeCanceledEventDTO : DefaultAdminDelayChangeCanceledEventDTOBase { }

    [Event("DefaultAdminDelayChangeCanceled")]
    public class DefaultAdminDelayChangeCanceledEventDTOBase : IEventDTO
    {
    }

    public partial class DefaultAdminDelayChangeScheduledEventDTO : DefaultAdminDelayChangeScheduledEventDTOBase { }

    [Event("DefaultAdminDelayChangeScheduled")]
    public class DefaultAdminDelayChangeScheduledEventDTOBase : IEventDTO
    {
        [Parameter("uint48", "newDelay", 1, false )]
        public virtual ulong NewDelay { get; set; }
        [Parameter("uint48", "effectSchedule", 2, false )]
        public virtual ulong EffectSchedule { get; set; }
    }

    public partial class DefaultAdminTransferCanceledEventDTO : DefaultAdminTransferCanceledEventDTOBase { }

    [Event("DefaultAdminTransferCanceled")]
    public class DefaultAdminTransferCanceledEventDTOBase : IEventDTO
    {
    }

    public partial class DefaultAdminTransferScheduledEventDTO : DefaultAdminTransferScheduledEventDTOBase { }

    [Event("DefaultAdminTransferScheduled")]
    public class DefaultAdminTransferScheduledEventDTOBase : IEventDTO
    {
        [Parameter("address", "newAdmin", 1, true )]
        public virtual string NewAdmin { get; set; }
        [Parameter("uint48", "acceptSchedule", 2, false )]
        public virtual ulong AcceptSchedule { get; set; }
    }

    public partial class EIP712DomainChangedEventDTO : EIP712DomainChangedEventDTOBase { }

    [Event("EIP712DomainChanged")]
    public class EIP712DomainChangedEventDTOBase : IEventDTO
    {
    }

    public partial class InitializedEventDTO : InitializedEventDTOBase { }

    [Event("Initialized")]
    public class InitializedEventDTOBase : IEventDTO
    {
        [Parameter("uint64", "version", 1, false )]
        public virtual ulong Version { get; set; }
    }

    public partial class MintedEventDTO : MintedEventDTOBase { }

    [Event("Minted")]
    public class MintedEventDTOBase : IEventDTO
    {
        [Parameter("address", "buyer", 1, true )]
        public virtual string Buyer { get; set; }
        [Parameter("uint256", "tierId", 2, true )]
        public virtual BigInteger TierId { get; set; }
        [Parameter("uint256", "nonce", 3, true )]
        public virtual BigInteger Nonce { get; set; }
        [Parameter("uint256", "firstTokenId", 4, false )]
        public virtual BigInteger FirstTokenId { get; set; }
        [Parameter("uint256", "quantity", 5, false )]
        public virtual BigInteger Quantity { get; set; }
    }

    public partial class OrderRejectedEventDTO : OrderRejectedEventDTOBase { }

    [Event("OrderRejected")]
    public class OrderRejectedEventDTOBase : IEventDTO
    {
        [Parameter("address", "buyer", 1, true )]
        public virtual string Buyer { get; set; }
        [Parameter("uint256", "nonce", 2, true )]
        public virtual BigInteger Nonce { get; set; }
        [Parameter("uint8", "reason", 3, false )]
        public virtual byte Reason { get; set; }
    }

    public partial class PausedEventDTO : PausedEventDTOBase { }

    [Event("Paused")]
    public class PausedEventDTOBase : IEventDTO
    {
        [Parameter("address", "account", 1, false )]
        public virtual string Account { get; set; }
    }

    public partial class RedeemedEventDTO : RedeemedEventDTOBase { }

    [Event("Redeemed")]
    public class RedeemedEventDTOBase : IEventDTO
    {
        [Parameter("uint256", "tokenId", 1, true )]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("address", "holder", 2, true )]
        public virtual string Holder { get; set; }
    }

    public partial class RedemptionsCheckpointedEventDTO : RedemptionsCheckpointedEventDTOBase { }

    [Event("RedemptionsCheckpointed")]
    public class RedemptionsCheckpointedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "root", 1, true )]
        public virtual byte[] Root { get; set; }
        [Parameter("uint256", "count", 2, false )]
        public virtual BigInteger Count { get; set; }
        [Parameter("uint256", "checkpointId", 3, true )]
        public virtual BigInteger CheckpointId { get; set; }
    }

    public partial class ResaleContractSetEventDTO : ResaleContractSetEventDTOBase { }

    [Event("ResaleContractSet")]
    public class ResaleContractSetEventDTOBase : IEventDTO
    {
        [Parameter("address", "resaleContract", 1, true )]
        public virtual string ResaleContract { get; set; }
    }

    public partial class RoleAdminChangedEventDTO : RoleAdminChangedEventDTOBase { }

    [Event("RoleAdminChanged")]
    public class RoleAdminChangedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "role", 1, true )]
        public virtual byte[] Role { get; set; }
        [Parameter("bytes32", "previousAdminRole", 2, true )]
        public virtual byte[] PreviousAdminRole { get; set; }
        [Parameter("bytes32", "newAdminRole", 3, true )]
        public virtual byte[] NewAdminRole { get; set; }
    }

    public partial class RoleGrantedEventDTO : RoleGrantedEventDTOBase { }

    [Event("RoleGranted")]
    public class RoleGrantedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "role", 1, true )]
        public virtual byte[] Role { get; set; }
        [Parameter("address", "account", 2, true )]
        public virtual string Account { get; set; }
        [Parameter("address", "sender", 3, true )]
        public virtual string Sender { get; set; }
    }

    public partial class RoleRevokedEventDTO : RoleRevokedEventDTOBase { }

    [Event("RoleRevoked")]
    public class RoleRevokedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "role", 1, true )]
        public virtual byte[] Role { get; set; }
        [Parameter("address", "account", 2, true )]
        public virtual string Account { get; set; }
        [Parameter("address", "sender", 3, true )]
        public virtual string Sender { get; set; }
    }

    public partial class TierConfiguredEventDTO : TierConfiguredEventDTOBase { }

    [Event("TierConfigured")]
    public class TierConfiguredEventDTOBase : IEventDTO
    {
        [Parameter("uint256", "tierId", 1, true )]
        public virtual BigInteger TierId { get; set; }
        [Parameter("string", "name", 2, false )]
        public virtual string Name { get; set; }
        [Parameter("uint256", "price", 3, false )]
        public virtual BigInteger Price { get; set; }
        [Parameter("uint256", "supplyCap", 4, false )]
        public virtual BigInteger SupplyCap { get; set; }
        [Parameter("uint256", "maxPerWallet", 5, false )]
        public virtual BigInteger MaxPerWallet { get; set; }
    }

    public partial class TransferEventDTO : TransferEventDTOBase { }

    [Event("Transfer")]
    public class TransferEventDTOBase : IEventDTO
    {
        [Parameter("address", "from", 1, true )]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2, true )]
        public virtual string To { get; set; }
        [Parameter("uint256", "tokenId", 3, true )]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class UnlockTimeSetEventDTO : UnlockTimeSetEventDTOBase { }

    [Event("UnlockTimeSet")]
    public class UnlockTimeSetEventDTOBase : IEventDTO
    {
        [Parameter("uint64", "unlockTime", 1, false )]
        public virtual ulong UnlockTime { get; set; }
    }

    public partial class UnpausedEventDTO : UnpausedEventDTOBase { }

    [Event("Unpaused")]
    public class UnpausedEventDTOBase : IEventDTO
    {
        [Parameter("address", "account", 1, false )]
        public virtual string Account { get; set; }
    }

    public partial class VoucherSignerSetEventDTO : VoucherSignerSetEventDTOBase { }

    [Event("VoucherSignerSet")]
    public class VoucherSignerSetEventDTOBase : IEventDTO
    {
        [Parameter("address", "signer", 1, true )]
        public virtual string Signer { get; set; }
    }

    public partial class AccessControlBadConfirmationError : AccessControlBadConfirmationErrorBase { }
    [Error("AccessControlBadConfirmation")]
    public class AccessControlBadConfirmationErrorBase : IErrorDTO
    {
    }

    public partial class AccessControlEnforcedDefaultAdminDelayError : AccessControlEnforcedDefaultAdminDelayErrorBase { }

    [Error("AccessControlEnforcedDefaultAdminDelay")]
    public class AccessControlEnforcedDefaultAdminDelayErrorBase : IErrorDTO
    {
        [Parameter("uint48", "schedule", 1)]
        public virtual ulong Schedule { get; set; }
    }

    public partial class AccessControlEnforcedDefaultAdminRulesError : AccessControlEnforcedDefaultAdminRulesErrorBase { }
    [Error("AccessControlEnforcedDefaultAdminRules")]
    public class AccessControlEnforcedDefaultAdminRulesErrorBase : IErrorDTO
    {
    }

    public partial class AccessControlInvalidDefaultAdminError : AccessControlInvalidDefaultAdminErrorBase { }

    [Error("AccessControlInvalidDefaultAdmin")]
    public class AccessControlInvalidDefaultAdminErrorBase : IErrorDTO
    {
        [Parameter("address", "defaultAdmin", 1)]
        public virtual string DefaultAdmin { get; set; }
    }

    public partial class AccessControlUnauthorizedAccountError : AccessControlUnauthorizedAccountErrorBase { }

    [Error("AccessControlUnauthorizedAccount")]
    public class AccessControlUnauthorizedAccountErrorBase : IErrorDTO
    {
        [Parameter("address", "account", 1)]
        public virtual string Account { get; set; }
        [Parameter("bytes32", "neededRole", 2)]
        public virtual byte[] NeededRole { get; set; }
    }

    public partial class AlreadyRedeemedError : AlreadyRedeemedErrorBase { }

    [Error("AlreadyRedeemed")]
    public class AlreadyRedeemedErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class ERC2981InvalidDefaultRoyaltyError : ERC2981InvalidDefaultRoyaltyErrorBase { }

    [Error("ERC2981InvalidDefaultRoyalty")]
    public class ERC2981InvalidDefaultRoyaltyErrorBase : IErrorDTO
    {
        [Parameter("uint256", "numerator", 1)]
        public virtual BigInteger Numerator { get; set; }
        [Parameter("uint256", "denominator", 2)]
        public virtual BigInteger Denominator { get; set; }
    }

    public partial class ERC2981InvalidDefaultRoyaltyReceiverError : ERC2981InvalidDefaultRoyaltyReceiverErrorBase { }

    [Error("ERC2981InvalidDefaultRoyaltyReceiver")]
    public class ERC2981InvalidDefaultRoyaltyReceiverErrorBase : IErrorDTO
    {
        [Parameter("address", "receiver", 1)]
        public virtual string Receiver { get; set; }
    }

    public partial class ERC2981InvalidTokenRoyaltyError : ERC2981InvalidTokenRoyaltyErrorBase { }

    [Error("ERC2981InvalidTokenRoyalty")]
    public class ERC2981InvalidTokenRoyaltyErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("uint256", "numerator", 2)]
        public virtual BigInteger Numerator { get; set; }
        [Parameter("uint256", "denominator", 3)]
        public virtual BigInteger Denominator { get; set; }
    }

    public partial class ERC2981InvalidTokenRoyaltyReceiverError : ERC2981InvalidTokenRoyaltyReceiverErrorBase { }

    [Error("ERC2981InvalidTokenRoyaltyReceiver")]
    public class ERC2981InvalidTokenRoyaltyReceiverErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("address", "receiver", 2)]
        public virtual string Receiver { get; set; }
    }

    public partial class ERC721IncorrectOwnerError : ERC721IncorrectOwnerErrorBase { }

    [Error("ERC721IncorrectOwner")]
    public class ERC721IncorrectOwnerErrorBase : IErrorDTO
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("address", "owner", 3)]
        public virtual string Owner { get; set; }
    }

    public partial class ERC721InsufficientApprovalError : ERC721InsufficientApprovalErrorBase { }

    [Error("ERC721InsufficientApproval")]
    public class ERC721InsufficientApprovalErrorBase : IErrorDTO
    {
        [Parameter("address", "operator", 1)]
        public virtual string Operator { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class ERC721InvalidApproverError : ERC721InvalidApproverErrorBase { }

    [Error("ERC721InvalidApprover")]
    public class ERC721InvalidApproverErrorBase : IErrorDTO
    {
        [Parameter("address", "approver", 1)]
        public virtual string Approver { get; set; }
    }

    public partial class ERC721InvalidOperatorError : ERC721InvalidOperatorErrorBase { }

    [Error("ERC721InvalidOperator")]
    public class ERC721InvalidOperatorErrorBase : IErrorDTO
    {
        [Parameter("address", "operator", 1)]
        public virtual string Operator { get; set; }
    }

    public partial class ERC721InvalidOwnerError : ERC721InvalidOwnerErrorBase { }

    [Error("ERC721InvalidOwner")]
    public class ERC721InvalidOwnerErrorBase : IErrorDTO
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
    }

    public partial class ERC721InvalidReceiverError : ERC721InvalidReceiverErrorBase { }

    [Error("ERC721InvalidReceiver")]
    public class ERC721InvalidReceiverErrorBase : IErrorDTO
    {
        [Parameter("address", "receiver", 1)]
        public virtual string Receiver { get; set; }
    }

    public partial class ERC721InvalidSenderError : ERC721InvalidSenderErrorBase { }

    [Error("ERC721InvalidSender")]
    public class ERC721InvalidSenderErrorBase : IErrorDTO
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }
    }

    public partial class ERC721NonexistentTokenError : ERC721NonexistentTokenErrorBase { }

    [Error("ERC721NonexistentToken")]
    public class ERC721NonexistentTokenErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class EnforcedPauseError : EnforcedPauseErrorBase { }
    [Error("EnforcedPause")]
    public class EnforcedPauseErrorBase : IErrorDTO
    {
    }

    public partial class ExpectedPauseError : ExpectedPauseErrorBase { }
    [Error("ExpectedPause")]
    public class ExpectedPauseErrorBase : IErrorDTO
    {
    }

    public partial class InvalidCheckpointError : InvalidCheckpointErrorBase { }
    [Error("InvalidCheckpoint")]
    public class InvalidCheckpointErrorBase : IErrorDTO
    {
    }

    public partial class InvalidInitializationError : InvalidInitializationErrorBase { }
    [Error("InvalidInitialization")]
    public class InvalidInitializationErrorBase : IErrorDTO
    {
    }

    public partial class InvalidProofError : InvalidProofErrorBase { }
    [Error("InvalidProof")]
    public class InvalidProofErrorBase : IErrorDTO
    {
    }

    public partial class InvalidScheduleError : InvalidScheduleErrorBase { }
    [Error("InvalidSchedule")]
    public class InvalidScheduleErrorBase : IErrorDTO
    {
    }

    public partial class InvalidTierConfigError : InvalidTierConfigErrorBase { }
    [Error("InvalidTierConfig")]
    public class InvalidTierConfigErrorBase : IErrorDTO
    {
    }

    public partial class InvalidVoucherSignatureError : InvalidVoucherSignatureErrorBase { }
    [Error("InvalidVoucherSignature")]
    public class InvalidVoucherSignatureErrorBase : IErrorDTO
    {
    }

    public partial class MaxPerWalletExceededError : MaxPerWalletExceededErrorBase { }

    [Error("MaxPerWalletExceeded")]
    public class MaxPerWalletExceededErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
        [Parameter("address", "wallet", 2)]
        public virtual string Wallet { get; set; }
    }

    public partial class NotInitializingError : NotInitializingErrorBase { }
    [Error("NotInitializing")]
    public class NotInitializingErrorBase : IErrorDTO
    {
    }

    public partial class SafeCastOverflowedUintDowncastError : SafeCastOverflowedUintDowncastErrorBase { }

    [Error("SafeCastOverflowedUintDowncast")]
    public class SafeCastOverflowedUintDowncastErrorBase : IErrorDTO
    {
        [Parameter("uint8", "bits", 1)]
        public virtual byte Bits { get; set; }
        [Parameter("uint256", "value", 2)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class SoulboundError : SoulboundErrorBase { }

    [Error("Soulbound")]
    public class SoulboundErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class SupplyCapExceededError : SupplyCapExceededErrorBase { }

    [Error("SupplyCapExceeded")]
    public class SupplyCapExceededErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
    }

    public partial class TicketListedError : TicketListedErrorBase { }

    [Error("TicketListed")]
    public class TicketListedErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class TransferLockedError : TransferLockedErrorBase { }

    [Error("TransferLocked")]
    public class TransferLockedErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("uint64", "unlockTime", 2)]
        public virtual ulong UnlockTime { get; set; }
    }

    public partial class UnknownTierError : UnknownTierErrorBase { }

    [Error("UnknownTier")]
    public class UnknownTierErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
    }

    public partial class VoucherAlreadyUsedError : VoucherAlreadyUsedErrorBase { }

    [Error("VoucherAlreadyUsed")]
    public class VoucherAlreadyUsedErrorBase : IErrorDTO
    {
        [Parameter("address", "buyer", 1)]
        public virtual string Buyer { get; set; }
        [Parameter("uint256", "nonce", 2)]
        public virtual BigInteger Nonce { get; set; }
    }

    public partial class VoucherExpiredError : VoucherExpiredErrorBase { }

    [Error("VoucherExpired")]
    public class VoucherExpiredErrorBase : IErrorDTO
    {
        [Parameter("uint256", "deadline", 1)]
        public virtual BigInteger Deadline { get; set; }
    }

    public partial class ZeroAddressError : ZeroAddressErrorBase { }
    [Error("ZeroAddress")]
    public class ZeroAddressErrorBase : IErrorDTO
    {
    }

    public partial class ZeroQuantityError : ZeroQuantityErrorBase { }
    [Error("ZeroQuantity")]
    public class ZeroQuantityErrorBase : IErrorDTO
    {
    }

    public partial class DefaultAdminRoleOutputDTO : DefaultAdminRoleOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class MinterRoleOutputDTO : MinterRoleOutputDTOBase { }

    [FunctionOutput]
    public class MinterRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class MintVoucherTypehashOutputDTO : MintVoucherTypehashOutputDTOBase { }

    [FunctionOutput]
    public class MintVoucherTypehashOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class PauserRoleOutputDTO : PauserRoleOutputDTOBase { }

    [FunctionOutput]
    public class PauserRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class RedeemerRoleOutputDTO : RedeemerRoleOutputDTOBase { }

    [FunctionOutput]
    public class RedeemerRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }





    public partial class BadgesClaimedOutputDTO : BadgesClaimedOutputDTOBase { }

    [FunctionOutput]
    public class BadgesClaimedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class BalanceOfOutputDTO : BalanceOfOutputDTOBase { }

    [FunctionOutput]
    public class BalanceOfOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }







    public partial class CheckpointCountOutputDTO : CheckpointCountOutputDTOBase { }

    [FunctionOutput]
    public class CheckpointCountOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }







    public partial class DefaultAdminOutputDTO : DefaultAdminOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class DefaultAdminDelayOutputDTO : DefaultAdminDelayOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminDelayOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint48", "", 1)]
        public virtual ulong ReturnValue1 { get; set; }
    }

    public partial class DefaultAdminDelayIncreaseWaitOutputDTO : DefaultAdminDelayIncreaseWaitOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminDelayIncreaseWaitOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint48", "", 1)]
        public virtual ulong ReturnValue1 { get; set; }
    }

    public partial class Eip712DomainOutputDTO : Eip712DomainOutputDTOBase { }

    [FunctionOutput]
    public class Eip712DomainOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes1", "fields", 1)]
        public virtual byte[] Fields { get; set; }
        [Parameter("string", "name", 2)]
        public virtual string Name { get; set; }
        [Parameter("string", "version", 3)]
        public virtual string Version { get; set; }
        [Parameter("uint256", "chainId", 4)]
        public virtual BigInteger ChainId { get; set; }
        [Parameter("address", "verifyingContract", 5)]
        public virtual string VerifyingContract { get; set; }
        [Parameter("bytes32", "salt", 6)]
        public virtual byte[] Salt { get; set; }
        [Parameter("uint256[]", "extensions", 7)]
        public virtual List<BigInteger> Extensions { get; set; }
    }

    public partial class EscrowOutputDTO : EscrowOutputDTOBase { }

    [FunctionOutput]
    public class EscrowOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class EventEndOutputDTO : EventEndOutputDTOBase { }

    [FunctionOutput]
    public class EventEndOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint64", "", 1)]
        public virtual ulong ReturnValue1 { get; set; }
    }

    public partial class EventStartOutputDTO : EventStartOutputDTOBase { }

    [FunctionOutput]
    public class EventStartOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint64", "", 1)]
        public virtual ulong ReturnValue1 { get; set; }
    }

    public partial class GetApprovedOutputDTO : GetApprovedOutputDTOBase { }

    [FunctionOutput]
    public class GetApprovedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class GetRoleAdminOutputDTO : GetRoleAdminOutputDTOBase { }

    [FunctionOutput]
    public class GetRoleAdminOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }



    public partial class HasRoleOutputDTO : HasRoleOutputDTOBase { }

    [FunctionOutput]
    public class HasRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class HashVoucherOutputDTO : HashVoucherOutputDTOBase { }

    [FunctionOutput]
    public class HashVoucherOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }



    public partial class IsApprovedForAllOutputDTO : IsApprovedForAllOutputDTOBase { }

    [FunctionOutput]
    public class IsApprovedForAllOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class IsRedeemedOutputDTO : IsRedeemedOutputDTOBase { }

    [FunctionOutput]
    public class IsRedeemedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class IsTransferableOutputDTO : IsTransferableOutputDTOBase { }

    [FunctionOutput]
    public class IsTransferableOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }





    public partial class MintedByOutputDTO : MintedByOutputDTOBase { }

    [FunctionOutput]
    public class MintedByOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class NameOutputDTO : NameOutputDTOBase { }

    [FunctionOutput]
    public class NameOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class NonceUsedOutputDTO : NonceUsedOutputDTOBase { }

    [FunctionOutput]
    public class NonceUsedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class OrganizerOutputDTO : OrganizerOutputDTOBase { }

    [FunctionOutput]
    public class OrganizerOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class OwnerOutputDTO : OwnerOutputDTOBase { }

    [FunctionOutput]
    public class OwnerOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class OwnerOfOutputDTO : OwnerOfOutputDTOBase { }

    [FunctionOutput]
    public class OwnerOfOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }



    public partial class PausedOutputDTO : PausedOutputDTOBase { }

    [FunctionOutput]
    public class PausedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class PendingDefaultAdminOutputDTO : PendingDefaultAdminOutputDTOBase { }

    [FunctionOutput]
    public class PendingDefaultAdminOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "newAdmin", 1)]
        public virtual string NewAdmin { get; set; }
        [Parameter("uint48", "schedule", 2)]
        public virtual ulong Schedule { get; set; }
    }

    public partial class PendingDefaultAdminDelayOutputDTO : PendingDefaultAdminDelayOutputDTOBase { }

    [FunctionOutput]
    public class PendingDefaultAdminDelayOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint48", "newDelay", 1)]
        public virtual ulong NewDelay { get; set; }
        [Parameter("uint48", "schedule", 2)]
        public virtual ulong Schedule { get; set; }
    }

    public partial class RedemptionCountOutputDTO : RedemptionCountOutputDTOBase { }

    [FunctionOutput]
    public class RedemptionCountOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class RedemptionRootOutputDTO : RedemptionRootOutputDTOBase { }

    [FunctionOutput]
    public class RedemptionRootOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class RendererOutputDTO : RendererOutputDTOBase { }

    [FunctionOutput]
    public class RendererOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }



    public partial class ResaleContractOutputDTO : ResaleContractOutputDTOBase { }

    [FunctionOutput]
    public class ResaleContractOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }





    public partial class RoyaltyInfoOutputDTO : RoyaltyInfoOutputDTOBase { }

    [FunctionOutput]
    public class RoyaltyInfoOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "receiver", 1)]
        public virtual string Receiver { get; set; }
        [Parameter("uint256", "amount", 2)]
        public virtual BigInteger Amount { get; set; }
    }













    public partial class SupportsInterfaceOutputDTO : SupportsInterfaceOutputDTOBase { }

    [FunctionOutput]
    public class SupportsInterfaceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class SymbolOutputDTO : SymbolOutputDTOBase { }

    [FunctionOutput]
    public class SymbolOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class TierConfigOutputDTO : TierConfigOutputDTOBase { }

    [FunctionOutput]
    public class TierConfigOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "tierName", 1)]
        public virtual string TierName { get; set; }
        [Parameter("uint256", "price", 2)]
        public virtual BigInteger Price { get; set; }
        [Parameter("uint256", "supplyCap", 3)]
        public virtual BigInteger SupplyCap { get; set; }
        [Parameter("uint256", "minted", 4)]
        public virtual BigInteger Minted { get; set; }
        [Parameter("uint256", "maxPerWallet", 5)]
        public virtual BigInteger MaxPerWallet { get; set; }
    }

    public partial class TierOfOutputDTO : TierOfOutputDTOBase { }

    [FunctionOutput]
    public class TierOfOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class TierPriceOutputDTO : TierPriceOutputDTOBase { }

    [FunctionOutput]
    public class TierPriceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class TokenURIOutputDTO : TokenURIOutputDTOBase { }

    [FunctionOutput]
    public class TokenURIOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class TotalMintedOutputDTO : TotalMintedOutputDTOBase { }

    [FunctionOutput]
    public class TotalMintedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }



    public partial class UnlockTimeOutputDTO : UnlockTimeOutputDTOBase { }

    [FunctionOutput]
    public class UnlockTimeOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint64", "", 1)]
        public virtual ulong ReturnValue1 { get; set; }
    }



    public partial class VoucherSignerOutputDTO : VoucherSignerOutputDTOBase { }

    [FunctionOutput]
    public class VoucherSignerOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }
}
