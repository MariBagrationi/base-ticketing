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

namespace TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition
{


    public partial class EventEscrowDeployment : EventEscrowDeploymentBase
    {
        public EventEscrowDeployment() : base(BYTECODE) { }
        public EventEscrowDeployment(string byteCode) : base(byteCode) { }
    }

    public class EventEscrowDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x";
        public EventEscrowDeploymentBase() : base(BYTECODE) { }
        public EventEscrowDeploymentBase(string byteCode) : base(byteCode) { }
        [Parameter("address", "usdc_", 1)]
        public virtual string Usdc { get; set; }
        [Parameter("address", "admin", 2)]
        public virtual string Admin { get; set; }
        [Parameter("uint48", "adminDelay", 3)]
        public virtual ulong AdminDelay { get; set; }
        [Parameter("address", "registrar", 4)]
        public virtual string Registrar { get; set; }
        [Parameter("address", "canceller", 5)]
        public virtual string Canceller { get; set; }
    }

    public partial class CancellerRoleFunction : CancellerRoleFunctionBase { }

    [Function("CANCELLER_ROLE", "bytes32")]
    public class CancellerRoleFunctionBase : FunctionMessage
    {

    }

    public partial class DefaultAdminRoleFunction : DefaultAdminRoleFunctionBase { }

    [Function("DEFAULT_ADMIN_ROLE", "bytes32")]
    public class DefaultAdminRoleFunctionBase : FunctionMessage
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

    public partial class CancelDefaultAdminTransferFunction : CancelDefaultAdminTransferFunctionBase { }

    [Function("cancelDefaultAdminTransfer")]
    public class CancelDefaultAdminTransferFunctionBase : FunctionMessage
    {

    }

    public partial class CancelEventFunction : CancelEventFunctionBase { }

    [Function("cancelEvent")]
    public class CancelEventFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class ChangeDefaultAdminDelayFunction : ChangeDefaultAdminDelayFunctionBase { }

    [Function("changeDefaultAdminDelay")]
    public class ChangeDefaultAdminDelayFunctionBase : FunctionMessage
    {
        [Parameter("uint48", "newDelay", 1)]
        public virtual ulong NewDelay { get; set; }
    }

    public partial class ClaimRefundFunction : ClaimRefundFunctionBase { }

    [Function("claimRefund")]
    public class ClaimRefundFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class CollectPaymentFunction : CollectPaymentFunctionBase { }

    [Function("collectPayment")]
    public class CollectPaymentFunctionBase : FunctionMessage
    {
        [Parameter("address", "payer", 1)]
        public virtual string Payer { get; set; }
        [Parameter("address", "buyer", 2)]
        public virtual string Buyer { get; set; }
        [Parameter("uint256", "firstTokenId", 3)]
        public virtual BigInteger FirstTokenId { get; set; }
        [Parameter("uint256", "quantity", 4)]
        public virtual BigInteger Quantity { get; set; }
        [Parameter("uint256", "unitPrice", 5)]
        public virtual BigInteger UnitPrice { get; set; }
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

    public partial class EventAccountFunction : EventAccountFunctionBase { }

    [Function("eventAccount", typeof(EventAccountOutputDTO))]
    public class EventAccountFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
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

    public partial class IsCancelledFunction : IsCancelledFunctionBase { }

    [Function("isCancelled", "bool")]
    public class IsCancelledFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class OwnerFunction : OwnerFunctionBase { }

    [Function("owner", "address")]
    public class OwnerFunctionBase : FunctionMessage
    {

    }

    public partial class PaidForFunction : PaidForFunctionBase { }

    [Function("paidFor", "uint256")]
    public class PaidForFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 2)]
        public virtual BigInteger TokenId { get; set; }
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

    public partial class PurchaseWithAuthorizationFunction : PurchaseWithAuthorizationFunctionBase { }

    [Function("purchaseWithAuthorization", "uint256")]
    public class PurchaseWithAuthorizationFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("tuple", "voucher", 2)]
        public virtual MintVoucher Voucher { get; set; }
        [Parameter("bytes", "voucherSignature", 3)]
        public virtual byte[] VoucherSignature { get; set; }
        [Parameter("tuple", "auth", 4)]
        public virtual PaymentAuthorization Auth { get; set; }
    }

    public partial class PurchaseWithPermitFunction : PurchaseWithPermitFunctionBase { }

    [Function("purchaseWithPermit", "uint256")]
    public class PurchaseWithPermitFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("tuple", "voucher", 2)]
        public virtual MintVoucher Voucher { get; set; }
        [Parameter("bytes", "voucherSignature", 3)]
        public virtual byte[] VoucherSignature { get; set; }
        [Parameter("tuple", "permit", 4)]
        public virtual PermitSignature Permit { get; set; }
    }

    public partial class RefundBatchFunction : RefundBatchFunctionBase { }

    [Function("refundBatch")]
    public class RefundBatchFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("uint256[]", "tokenIds", 2)]
        public virtual List<BigInteger> TokenIds { get; set; }
    }

    public partial class RegisterEventFunction : RegisterEventFunctionBase { }

    [Function("registerEvent")]
    public class RegisterEventFunctionBase : FunctionMessage
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class ReleaseFunction : ReleaseFunctionBase { }

    [Function("release")]
    public class ReleaseFunctionBase : FunctionMessage
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

    public partial class SupportsInterfaceFunction : SupportsInterfaceFunctionBase { }

    [Function("supportsInterface", "bool")]
    public class SupportsInterfaceFunctionBase : FunctionMessage
    {
        [Parameter("bytes4", "interfaceId", 1)]
        public virtual byte[] InterfaceId { get; set; }
    }

    public partial class TotalHeldFunction : TotalHeldFunctionBase { }

    [Function("totalHeld", "uint256")]
    public class TotalHeldFunctionBase : FunctionMessage
    {

    }

    public partial class UsdcFunction : UsdcFunctionBase { }

    [Function("usdc", "address")]
    public class UsdcFunctionBase : FunctionMessage
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

    public partial class EscrowReleasedEventDTO : EscrowReleasedEventDTOBase { }

    [Event("EscrowReleased")]
    public class EscrowReleasedEventDTOBase : IEventDTO
    {
        [Parameter("address", "ticket", 1, true )]
        public virtual string Ticket { get; set; }
        [Parameter("address", "organizer", 2, true )]
        public virtual string Organizer { get; set; }
        [Parameter("uint256", "amount", 3, false )]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class EventCancelledEventDTO : EventCancelledEventDTOBase { }

    [Event("EventCancelled")]
    public class EventCancelledEventDTOBase : IEventDTO
    {
        [Parameter("address", "ticket", 1, true )]
        public virtual string Ticket { get; set; }
        [Parameter("address", "cancelledBy", 2, true )]
        public virtual string CancelledBy { get; set; }
    }

    public partial class EventRegisteredEventDTO : EventRegisteredEventDTOBase { }

    [Event("EventRegistered")]
    public class EventRegisteredEventDTOBase : IEventDTO
    {
        [Parameter("address", "ticket", 1, true )]
        public virtual string Ticket { get; set; }
    }

    public partial class PaymentDepositedEventDTO : PaymentDepositedEventDTOBase { }

    [Event("PaymentDeposited")]
    public class PaymentDepositedEventDTOBase : IEventDTO
    {
        [Parameter("address", "ticket", 1, true )]
        public virtual string Ticket { get; set; }
        [Parameter("address", "buyer", 2, true )]
        public virtual string Buyer { get; set; }
        [Parameter("address", "payer", 3, true )]
        public virtual string Payer { get; set; }
        [Parameter("uint256", "firstTokenId", 4, false )]
        public virtual BigInteger FirstTokenId { get; set; }
        [Parameter("uint256", "quantity", 5, false )]
        public virtual BigInteger Quantity { get; set; }
        [Parameter("uint256", "amount", 6, false )]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class RefundedEventDTO : RefundedEventDTOBase { }

    [Event("Refunded")]
    public class RefundedEventDTOBase : IEventDTO
    {
        [Parameter("address", "ticket", 1, true )]
        public virtual string Ticket { get; set; }
        [Parameter("uint256", "tokenId", 2, true )]
        public virtual BigInteger TokenId { get; set; }
        [Parameter("address", "holder", 3, true )]
        public virtual string Holder { get; set; }
        [Parameter("uint256", "amount", 4, false )]
        public virtual BigInteger Amount { get; set; }
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

    public partial class AlreadyReleasedError : AlreadyReleasedErrorBase { }

    [Error("AlreadyReleased")]
    public class AlreadyReleasedErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class EventIsCancelledError : EventIsCancelledErrorBase { }

    [Error("EventIsCancelled")]
    public class EventIsCancelledErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class EventNotCancelledError : EventNotCancelledErrorBase { }

    [Error("EventNotCancelled")]
    public class EventNotCancelledErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class EventNotEndedError : EventNotEndedErrorBase { }

    [Error("EventNotEnded")]
    public class EventNotEndedErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
        [Parameter("uint64", "eventEnd", 2)]
        public virtual ulong EventEnd { get; set; }
    }

    public partial class NotAuthorizedError : NotAuthorizedErrorBase { }
    [Error("NotAuthorized")]
    public class NotAuthorizedErrorBase : IErrorDTO
    {
    }

    public partial class NotRegisteredError : NotRegisteredErrorBase { }

    [Error("NotRegistered")]
    public class NotRegisteredErrorBase : IErrorDTO
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }

    public partial class NothingToRefundError : NothingToRefundErrorBase { }

    [Error("NothingToRefund")]
    public class NothingToRefundErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class NothingToReleaseError : NothingToReleaseErrorBase { }
    [Error("NothingToRelease")]
    public class NothingToReleaseErrorBase : IErrorDTO
    {
    }

    public partial class PrepaidMismatchError : PrepaidMismatchErrorBase { }
    [Error("PrepaidMismatch")]
    public class PrepaidMismatchErrorBase : IErrorDTO
    {
    }

    public partial class ReentrancyGuardReentrantCallError : ReentrancyGuardReentrantCallErrorBase { }
    [Error("ReentrancyGuardReentrantCall")]
    public class ReentrancyGuardReentrantCallErrorBase : IErrorDTO
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

    public partial class TicketListedError : TicketListedErrorBase { }

    [Error("TicketListed")]
    public class TicketListedErrorBase : IErrorDTO
    {
        [Parameter("uint256", "tokenId", 1)]
        public virtual BigInteger TokenId { get; set; }
    }

    public partial class ZeroAddressError : ZeroAddressErrorBase { }
    [Error("ZeroAddress")]
    public class ZeroAddressErrorBase : IErrorDTO
    {
    }

    public partial class CancellerRoleOutputDTO : CancellerRoleOutputDTOBase { }

    [FunctionOutput]
    public class CancellerRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class DefaultAdminRoleOutputDTO : DefaultAdminRoleOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
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

    public partial class EventAccountOutputDTO : EventAccountOutputDTOBase { }

    [FunctionOutput]
    public class EventAccountOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "registered", 1)]
        public virtual bool Registered { get; set; }
        [Parameter("bool", "cancelled", 2)]
        public virtual bool Cancelled { get; set; }
        [Parameter("uint256", "held", 3)]
        public virtual BigInteger Held { get; set; }
        [Parameter("uint256", "released", 4)]
        public virtual BigInteger Released { get; set; }
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

    public partial class IsCancelledOutputDTO : IsCancelledOutputDTOBase { }

    [FunctionOutput]
    public class IsCancelledOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class OwnerOutputDTO : OwnerOutputDTOBase { }

    [FunctionOutput]
    public class OwnerOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class PaidForOutputDTO : PaidForOutputDTOBase { }

    [FunctionOutput]
    public class PaidForOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
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

















    public partial class SupportsInterfaceOutputDTO : SupportsInterfaceOutputDTOBase { }

    [FunctionOutput]
    public class SupportsInterfaceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class TotalHeldOutputDTO : TotalHeldOutputDTOBase { }

    [FunctionOutput]
    public class TotalHeldOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class UsdcOutputDTO : UsdcOutputDTOBase { }

    [FunctionOutput]
    public class UsdcOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }
}
