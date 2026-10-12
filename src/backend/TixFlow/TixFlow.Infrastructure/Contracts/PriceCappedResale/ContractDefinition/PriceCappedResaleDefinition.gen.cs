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

namespace TixFlow.Infrastructure.Contracts.PriceCappedResale.ContractDefinition
{


    public partial class PriceCappedResaleDeployment : PriceCappedResaleDeploymentBase
    {
        public PriceCappedResaleDeployment() : base(BYTECODE) { }
        public PriceCappedResaleDeployment(string byteCode) : base(byteCode) { }
    }

    public class PriceCappedResaleDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x";
        public PriceCappedResaleDeploymentBase() : base(BYTECODE) { }
        public PriceCappedResaleDeploymentBase(string byteCode) : base(byteCode) { }
        [Parameter("address", "usdc_", 1)]
        public virtual string Usdc { get; set; }
        [Parameter("address", "escrow_", 2)]
        public virtual string Escrow { get; set; }
        [Parameter("address", "admin", 3)]
        public virtual string Admin { get; set; }
        [Parameter("uint48", "adminDelay", 4)]
        public virtual ulong AdminDelay { get; set; }
        [Parameter("uint16", "markupBps_", 5)]
        public virtual ushort Markupbps { get; set; }
        [Parameter("address", "registrar", 6)]
        public virtual string Registrar { get; set; }
    }

    public partial class BpsDenominatorFunction : BpsDenominatorFunctionBase { }

    [Function("BPS_DENOMINATOR", "uint16")]
    public class BpsDenominatorFunctionBase : FunctionMessage
    {

    }

    public partial class DefaultAdminRoleFunction : DefaultAdminRoleFunctionBase { }

    [Function("DEFAULT_ADMIN_ROLE", "bytes32")]
    public class DefaultAdminRoleFunctionBase : FunctionMessage
    {

    }

    public partial class MaxMarkupBpsFunction : MaxMarkupBpsFunctionBase { }

    [Function("MAX_MARKUP_BPS", "uint16")]
    public class MaxMarkupBpsFunctionBase : FunctionMessage
    {

    }

    public partial class MaxProtocolFeeBpsFunction : MaxProtocolFeeBpsFunctionBase { }

    [Function("MAX_PROTOCOL_FEE_BPS", "uint16")]
    public class MaxProtocolFeeBpsFunctionBase : FunctionMessage
    {

    }

    public partial class RegistrarRoleFunction : RegistrarRoleFunctionBase { }

    [Function("REGISTRAR_ROLE", "bytes32")]
    public class RegistrarRoleFunctionBase : FunctionMessage
    {

    }

    public partial class AcceptDefaultAdminTransferFunction : AcceptDefaultAdminTransferFunctionBase { }

    [Function("acceptDefaultAdminTransfer")]
    public class AcceptDefaultAdminTransferFunctionBase : FunctionMessage
    {

    }

    public partial class BeginDefaultAdminTransferFunction : BeginDefaultAdminTransferFunctionBase { }

    [Function("beginDefaultAdminTransfer")]
    public class BeginDefaultAdminTransferFunctionBase : FunctionMessage
    {
        [Parameter("address", "newAdmin", 1)]
        public virtual string NewAdmin { get; set; }
    }

    public partial class BuyFunction : BuyFunctionBase { }

    [Function("buy")]
    public class BuyFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "listingId", 1)]
        public virtual BigInteger ListingId { get; set; }
    }

    public partial class CancelFunction : CancelFunctionBase { }

    [Function("cancel")]
    public class CancelFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "listingId", 1)]
        public virtual BigInteger ListingId { get; set; }
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

    public partial class EscrowFunction : EscrowFunctionBase { }

    [Function("escrow", "address")]
    public class EscrowFunctionBase : FunctionMessage
    {

    }

    public partial class FeeRecipientFunction : FeeRecipientFunctionBase { }

    [Function("feeRecipient", "address")]
    public class FeeRecipientFunctionBase : FunctionMessage
    {

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

    public partial class IsRegisteredTicketFunction : IsRegisteredTicketFunctionBase { }

    [Function("isRegisteredTicket", "bool")]
    public class IsRegisteredTicketFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class ListFunction : ListFunctionBase { }

    [Function("list", "uint256")]
    public class ListFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("uint256", "price", 3)]
        public virtual BigInteger Price { get; set; }
    }

    public partial class ListingsFunction : ListingsFunctionBase { }

    [Function("listings", typeof(ListingsOutputDTO))]
    public class ListingsFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "listingId", 1)]
        public virtual BigInteger ListingId { get; set; }
    }

    public partial class MarkupBpsFunction : MarkupBpsFunctionBase { }

    [Function("markupBps", "uint16")]
    public class MarkupBpsFunctionBase : FunctionMessage
    {

    }

    public partial class MaxResalePriceFunction : MaxResalePriceFunctionBase { }

    [Function("maxResalePrice", "uint256")]
    public class MaxResalePriceFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class NextListingIdFunction : NextListingIdFunctionBase { }

    [Function("nextListingId", "uint256")]
    public class NextListingIdFunctionBase : FunctionMessage
    {

    }

    public partial class OwnerFunction : OwnerFunctionBase { }

    [Function("owner", "address")]
    public class OwnerFunctionBase : FunctionMessage
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

    public partial class PendingWithdrawalsFunction : PendingWithdrawalsFunctionBase { }

    [Function("pendingWithdrawals", "uint256")]
    public class PendingWithdrawalsFunctionBase : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public virtual string Account { get; set; }
    }

    public partial class ProtocolFeeBpsFunction : ProtocolFeeBpsFunctionBase { }

    [Function("protocolFeeBps", "uint16")]
    public class ProtocolFeeBpsFunctionBase : FunctionMessage
    {

    }

    public partial class RegisterTicketFunction : RegisterTicketFunctionBase { }

    [Function("registerTicket")]
    public class RegisterTicketFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
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

    public partial class SetMarkupBpsFunction : SetMarkupBpsFunctionBase { }

    [Function("setMarkupBps")]
    public class SetMarkupBpsFunctionBase : FunctionMessage
    {
        [Parameter("uint16", "newMarkupBps", 1)]
        public virtual ushort NewMarkupBps { get; set; }
    }

    public partial class SetProtocolFeeFunction : SetProtocolFeeFunctionBase { }

    [Function("setProtocolFee")]
    public class SetProtocolFeeFunctionBase : FunctionMessage
    {
        [Parameter("uint16", "newFeeBps", 1)]
        public virtual ushort NewFeeBps { get; set; }
        [Parameter("address", "newRecipient", 2)]
        public virtual string NewRecipient { get; set; }
    }

    public partial class SupportsInterfaceFunction : SupportsInterfaceFunctionBase { }

    [Function("supportsInterface", "bool")]
    public class SupportsInterfaceFunctionBase : FunctionMessage
    {
        [Parameter("bytes4", "interfaceId", 1)]
        public virtual byte[] InterfaceId { get; set; }
    }

    public partial class UsdcFunction : UsdcFunctionBase { }

    [Function("usdc", "address")]
    public class UsdcFunctionBase : FunctionMessage
    {

    }

    public partial class WithdrawFunction : WithdrawFunctionBase { }

    [Function("withdraw")]
    public class WithdrawFunctionBase : FunctionMessage
    {

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

    public partial class ListedEventDTO : ListedEventDTOBase { }

    [Event("Listed")]
    public class ListedEventDTOBase : IEventDTO
    {
        [Parameter("uint256", "listingId", 1, true )]
        public virtual BigInteger ListingId { get; set; }
        [Parameter("address", "ticket", 2, true )]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 3, true )]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("address", "seller", 4, false )]
        public virtual string Seller { get; set; }
        [Parameter("uint256", "price", 5, false )]
        public virtual BigInteger Price { get; set; }
    }

    public partial class ListingCancelledEventDTO : ListingCancelledEventDTOBase { }

    [Event("ListingCancelled")]
    public class ListingCancelledEventDTOBase : IEventDTO
    {
        [Parameter("uint256", "listingId", 1, true )]
        public virtual BigInteger ListingId { get; set; }
    }

    public partial class MarkupSetEventDTO : MarkupSetEventDTOBase { }

    [Event("MarkupSet")]
    public class MarkupSetEventDTOBase : IEventDTO
    {
        [Parameter("uint16", "markupBps", 1, false )]
        public virtual ushort MarkupBps { get; set; }
    }

    public partial class ProtocolFeeSetEventDTO : ProtocolFeeSetEventDTOBase { }

    [Event("ProtocolFeeSet")]
    public class ProtocolFeeSetEventDTOBase : IEventDTO
    {
        [Parameter("uint16", "protocolFeeBps", 1, false )]
        public virtual ushort ProtocolFeeBps { get; set; }
        [Parameter("address", "feeRecipient", 2, true )]
        public virtual string FeeRecipient { get; set; }
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

    public partial class SoldEventDTO : SoldEventDTOBase { }

    [Event("Sold")]
    public class SoldEventDTOBase : IEventDTO
    {
        [Parameter("uint256", "listingId", 1, true )]
        public virtual BigInteger ListingId { get; set; }
        [Parameter("address", "buyer", 2, true )]
        public virtual string Buyer { get; set; }
        [Parameter("address", "seller", 3, true )]
        public virtual string Seller { get; set; }
        [Parameter("uint256", "price", 4, false )]
        public virtual BigInteger Price { get; set; }
        [Parameter("uint256", "royalty", 5, false )]
        public virtual BigInteger Royalty { get; set; }
        [Parameter("uint256", "protocolFee", 6, false )]
        public virtual BigInteger ProtocolFee { get; set; }
    }

    public partial class TicketRegisteredEventDTO : TicketRegisteredEventDTOBase { }

    [Event("TicketRegistered")]
    public class TicketRegisteredEventDTOBase : IEventDTO
    {
        [Parameter("address", "ticket", 1, true )]
        public virtual string Ticket { get; set; }
    }

    public partial class WithdrawnEventDTO : WithdrawnEventDTOBase { }

    [Event("Withdrawn")]
    public class WithdrawnEventDTOBase : IEventDTO
    {
        [Parameter("address", "account", 1, true )]
        public virtual string Account { get; set; }
        [Parameter("uint256", "amount", 2, false )]
        public virtual BigInteger Amount { get; set; }
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

    public partial class AlreadyRegisteredError : AlreadyRegisteredErrorBase { }

    [Error("AlreadyRegistered")]
    public class AlreadyRegisteredErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class InvalidBpsError : InvalidBpsErrorBase { }
    [Error("InvalidBps")]
    public class InvalidBpsErrorBase : IErrorDTO
    {
    }

    public partial class ListingNotActiveError : ListingNotActiveErrorBase { }

    [Error("ListingNotActive")]
    public class ListingNotActiveErrorBase : IErrorDTO
    {
        [Parameter("uint256", "listingId", 1)]
        public virtual BigInteger ListingId { get; set; }
    }

    public partial class NotRegisteredError : NotRegisteredErrorBase { }

    [Error("NotRegistered")]
    public class NotRegisteredErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class NotSellerError : NotSellerErrorBase { }
    [Error("NotSeller")]
    public class NotSellerErrorBase : IErrorDTO
    {
    }

    public partial class NotTicketOwnerError : NotTicketOwnerErrorBase { }
    [Error("NotTicketOwner")]
    public class NotTicketOwnerErrorBase : IErrorDTO
    {
    }

    public partial class NothingToWithdrawError : NothingToWithdrawErrorBase { }
    [Error("NothingToWithdraw")]
    public class NothingToWithdrawErrorBase : IErrorDTO
    {
    }

    public partial class PriceAboveCapError : PriceAboveCapErrorBase { }

    [Error("PriceAboveCap")]
    public class PriceAboveCapErrorBase : IErrorDTO
    {
        [Parameter("uint256", "price", 1)]
        public virtual BigInteger Price { get; set; }
        [Parameter("uint256", "cap", 2)]
        public virtual BigInteger Cap { get; set; }
    }

    public partial class ReentrancyGuardReentrantCallError : ReentrancyGuardReentrantCallErrorBase { }
    [Error("ReentrancyGuardReentrantCall")]
    public class ReentrancyGuardReentrantCallErrorBase : IErrorDTO
    {
    }

    public partial class ResaleClosedError : ResaleClosedErrorBase { }
    [Error("ResaleClosed")]
    public class ResaleClosedErrorBase : IErrorDTO
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

    public partial class SafeERC20FailedOperationError : SafeERC20FailedOperationErrorBase { }

    [Error("SafeERC20FailedOperation")]
    public class SafeERC20FailedOperationErrorBase : IErrorDTO
    {
        [Parameter("address", "token", 1)]
        public virtual string Token { get; set; }
    }

    public partial class TicketRedeemedError : TicketRedeemedErrorBase { }

    [Error("TicketRedeemed")]
    public class TicketRedeemedErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class ZeroAddressError : ZeroAddressErrorBase { }
    [Error("ZeroAddress")]
    public class ZeroAddressErrorBase : IErrorDTO
    {
    }

    public partial class ZeroPriceError : ZeroPriceErrorBase { }
    [Error("ZeroPrice")]
    public class ZeroPriceErrorBase : IErrorDTO
    {
    }

    public partial class BpsDenominatorOutputDTO : BpsDenominatorOutputDTOBase { }

    [FunctionOutput]
    public class BpsDenominatorOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint16", "", 1)]
        public virtual ushort ReturnValue1 { get; set; }
    }

    public partial class DefaultAdminRoleOutputDTO : DefaultAdminRoleOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class MaxMarkupBpsOutputDTO : MaxMarkupBpsOutputDTOBase { }

    [FunctionOutput]
    public class MaxMarkupBpsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint16", "", 1)]
        public virtual ushort ReturnValue1 { get; set; }
    }

    public partial class MaxProtocolFeeBpsOutputDTO : MaxProtocolFeeBpsOutputDTOBase { }

    [FunctionOutput]
    public class MaxProtocolFeeBpsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint16", "", 1)]
        public virtual ushort ReturnValue1 { get; set; }
    }

    public partial class RegistrarRoleOutputDTO : RegistrarRoleOutputDTOBase { }

    [FunctionOutput]
    public class RegistrarRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
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

    public partial class EscrowOutputDTO : EscrowOutputDTOBase { }

    [FunctionOutput]
    public class EscrowOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class FeeRecipientOutputDTO : FeeRecipientOutputDTOBase { }

    [FunctionOutput]
    public class FeeRecipientOutputDTOBase : IFunctionOutputDTO 
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

    public partial class IsRegisteredTicketOutputDTO : IsRegisteredTicketOutputDTOBase { }

    [FunctionOutput]
    public class IsRegisteredTicketOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }



    public partial class ListingsOutputDTO : ListingsOutputDTOBase { }

    [FunctionOutput]
    public class ListingsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "seller", 1)]
        public virtual string Seller { get; set; }
        [Parameter("address", "ticket", 2)]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 3)]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("uint256", "price", 4)]
        public virtual BigInteger Price { get; set; }
        [Parameter("bool", "active", 5)]
        public virtual bool Active { get; set; }
    }

    public partial class MarkupBpsOutputDTO : MarkupBpsOutputDTOBase { }

    [FunctionOutput]
    public class MarkupBpsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint16", "", 1)]
        public virtual ushort ReturnValue1 { get; set; }
    }

    public partial class MaxResalePriceOutputDTO : MaxResalePriceOutputDTOBase { }

    [FunctionOutput]
    public class MaxResalePriceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class NextListingIdOutputDTO : NextListingIdOutputDTOBase { }

    [FunctionOutput]
    public class NextListingIdOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class OwnerOutputDTO : OwnerOutputDTOBase { }

    [FunctionOutput]
    public class OwnerOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
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

    public partial class PendingWithdrawalsOutputDTO : PendingWithdrawalsOutputDTOBase { }

    [FunctionOutput]
    public class PendingWithdrawalsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class ProtocolFeeBpsOutputDTO : ProtocolFeeBpsOutputDTOBase { }

    [FunctionOutput]
    public class ProtocolFeeBpsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint16", "", 1)]
        public virtual ushort ReturnValue1 { get; set; }
    }













    public partial class SupportsInterfaceOutputDTO : SupportsInterfaceOutputDTOBase { }

    [FunctionOutput]
    public class SupportsInterfaceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class UsdcOutputDTO : UsdcOutputDTOBase { }

    [FunctionOutput]
    public class UsdcOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }


}
