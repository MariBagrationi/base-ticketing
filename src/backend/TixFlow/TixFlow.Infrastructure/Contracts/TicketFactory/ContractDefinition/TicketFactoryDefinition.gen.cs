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

namespace TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition
{


    public partial class TicketFactoryDeployment : TicketFactoryDeploymentBase
    {
        public TicketFactoryDeployment() : base(BYTECODE) { }
        public TicketFactoryDeployment(string byteCode) : base(byteCode) { }
    }

    public class TicketFactoryDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x";
        public TicketFactoryDeploymentBase() : base(BYTECODE) { }
        public TicketFactoryDeploymentBase(string byteCode) : base(byteCode) { }
        [Parameter("address", "implementation_", 1)]
        public virtual string Implementation { get; set; }
        [Parameter("address", "escrow_", 2)]
        public virtual string Escrow { get; set; }
        [Parameter("address", "resale_", 3)]
        public virtual string Resale { get; set; }
        [Parameter("address", "renderer_", 4)]
        public virtual string Renderer { get; set; }
        [Parameter("tuple", "config", 5)]
        public virtual PlatformConfig Config { get; set; }
        [Parameter("address", "eventCreator", 6)]
        public virtual string EventCreator { get; set; }
    }

    public partial class DefaultAdminRoleFunction : DefaultAdminRoleFunctionBase { }

    [Function("DEFAULT_ADMIN_ROLE", "bytes32")]
    public class DefaultAdminRoleFunctionBase : FunctionMessage
    {

    }

    public partial class EventCreatorRoleFunction : EventCreatorRoleFunctionBase { }

    [Function("EVENT_CREATOR_ROLE", "bytes32")]
    public class EventCreatorRoleFunctionBase : FunctionMessage
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

    public partial class ChangeDefaultAdminDelayFunction : ChangeDefaultAdminDelayFunctionBase { }

    [Function("changeDefaultAdminDelay")]
    public class ChangeDefaultAdminDelayFunctionBase : FunctionMessage
    {
        [Parameter("uint48", "newDelay", 1)]
        public virtual ulong NewDelay { get; set; }
    }

    public partial class CreateEventFunction : CreateEventFunctionBase { }

    [Function("createEvent", "address")]
    public class CreateEventFunctionBase : FunctionMessage
    {
        [Parameter("tuple", "p", 1)]
        public virtual CreateEventParams P { get; set; }
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

    public partial class EventCountFunction : EventCountFunctionBase { }

    [Function("eventCount", "uint256")]
    public class EventCountFunctionBase : FunctionMessage
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

    public partial class ImplementationFunction : ImplementationFunctionBase { }

    [Function("implementation", "address")]
    public class ImplementationFunctionBase : FunctionMessage
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

    public partial class PlatformFunction : PlatformFunctionBase { }

    [Function("platform", typeof(PlatformOutputDTO))]
    public class PlatformFunctionBase : FunctionMessage
    {

    }

    public partial class PredictTicketAddressFunction : PredictTicketAddressFunctionBase { }

    [Function("predictTicketAddress", "address")]
    public class PredictTicketAddressFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "eventId", 1)]
        public virtual byte[] EventId { get; set; }
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

    public partial class ResaleFunction : ResaleFunctionBase { }

    [Function("resale", "address")]
    public class ResaleFunctionBase : FunctionMessage
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

    public partial class SetPlatformConfigFunction : SetPlatformConfigFunctionBase { }

    [Function("setPlatformConfig")]
    public class SetPlatformConfigFunctionBase : FunctionMessage
    {
        [Parameter("tuple", "config", 1)]
        public virtual PlatformConfig Config { get; set; }
    }

    public partial class SupportsInterfaceFunction : SupportsInterfaceFunctionBase { }

    [Function("supportsInterface", "bool")]
    public class SupportsInterfaceFunctionBase : FunctionMessage
    {
        [Parameter("bytes4", "interfaceId", 1)]
        public virtual byte[] InterfaceId { get; set; }
    }

    public partial class TicketOfFunction : TicketOfFunctionBase { }

    [Function("ticketOf", "address")]
    public class TicketOfFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "eventId", 1)]
        public virtual byte[] EventId { get; set; }
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

    public partial class EventCreatedEventDTO : EventCreatedEventDTOBase { }

    [Event("EventCreated")]
    public class EventCreatedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "eventId", 1, true )]
        public virtual byte[] EventId { get; set; }
        [Parameter("address", "ticket", 2, true )]
        public virtual string Ticket { get; set; }
        [Parameter("address", "organizer", 3, true )]
        public virtual string Organizer { get; set; }
        [Parameter("uint64", "eventStart", 4, false )]
        public virtual ulong EventStart { get; set; }
        [Parameter("uint64", "eventEnd", 5, false )]
        public virtual ulong EventEnd { get; set; }
    }

    public partial class PlatformConfigSetEventDTO : PlatformConfigSetEventDTOBase { }

    [Event("PlatformConfigSet")]
    public class PlatformConfigSetEventDTOBase : IEventDTO
    {
        [Parameter("tuple", "config", 1, false )]
        public virtual PlatformConfig Config { get; set; }
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

    public partial class FailedDeploymentError : FailedDeploymentErrorBase { }
    [Error("FailedDeployment")]
    public class FailedDeploymentErrorBase : IErrorDTO
    {
    }

    public partial class InsufficientBalanceError : InsufficientBalanceErrorBase { }

    [Error("InsufficientBalance")]
    public class InsufficientBalanceErrorBase : IErrorDTO
    {
        [Parameter("uint256", "balance", 1)]
        public virtual BigInteger Balance { get; set; }
        [Parameter("uint256", "needed", 2)]
        public virtual BigInteger Needed { get; set; }
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

    public partial class ZeroAddressError : ZeroAddressErrorBase { }
    [Error("ZeroAddress")]
    public class ZeroAddressErrorBase : IErrorDTO
    {
    }

    public partial class DefaultAdminRoleOutputDTO : DefaultAdminRoleOutputDTOBase { }

    [FunctionOutput]
    public class DefaultAdminRoleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class EventCreatorRoleOutputDTO : EventCreatorRoleOutputDTOBase { }

    [FunctionOutput]
    public class EventCreatorRoleOutputDTOBase : IFunctionOutputDTO 
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

    public partial class EventCountOutputDTO : EventCountOutputDTOBase { }

    [FunctionOutput]
    public class EventCountOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
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

    public partial class ImplementationOutputDTO : ImplementationOutputDTOBase { }

    [FunctionOutput]
    public class ImplementationOutputDTOBase : IFunctionOutputDTO 
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

    public partial class PlatformOutputDTO : PlatformOutputDTOBase { }

    [FunctionOutput]
    public class PlatformOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "admin", 1)]
        public virtual string Admin { get; set; }
        [Parameter("uint48", "adminDelay", 2)]
        public virtual ulong AdminDelay { get; set; }
        [Parameter("address", "minter", 3)]
        public virtual string Minter { get; set; }
        [Parameter("address", "redeemer", 4)]
        public virtual string Redeemer { get; set; }
        [Parameter("address", "pauser", 5)]
        public virtual string Pauser { get; set; }
        [Parameter("address", "voucherSigner", 6)]
        public virtual string VoucherSigner { get; set; }
    }

    public partial class PredictTicketAddressOutputDTO : PredictTicketAddressOutputDTOBase { }

    [FunctionOutput]
    public class PredictTicketAddressOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class RendererOutputDTO : RendererOutputDTOBase { }

    [FunctionOutput]
    public class RendererOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }



    public partial class ResaleOutputDTO : ResaleOutputDTOBase { }

    [FunctionOutput]
    public class ResaleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }







    public partial class SupportsInterfaceOutputDTO : SupportsInterfaceOutputDTOBase { }

    [FunctionOutput]
    public class SupportsInterfaceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class TicketOfOutputDTO : TicketOfOutputDTOBase { }

    [FunctionOutput]
    public class TicketOfOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "ticket", 1)]
        public virtual string Ticket { get; set; }
    }
}
