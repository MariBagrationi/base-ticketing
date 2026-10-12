using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Contracts;
using System.Threading;
using TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition;

namespace TixFlow.Infrastructure.Contracts.TicketFactory
{
    public partial class TicketFactoryService: TicketFactoryServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, TicketFactoryDeployment ticketFactoryDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<TicketFactoryDeployment>().SendRequestAndWaitForReceiptAsync(ticketFactoryDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, TicketFactoryDeployment ticketFactoryDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<TicketFactoryDeployment>().SendRequestAsync(ticketFactoryDeployment);
        }

        public static async Task<TicketFactoryService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, TicketFactoryDeployment ticketFactoryDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, ticketFactoryDeployment, cancellationTokenSource);
            return new TicketFactoryService(web3, receipt.ContractAddress);
        }

        public TicketFactoryService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

    }


    public partial class TicketFactoryServiceBase: ContractWeb3ServiceBase
    {

        public TicketFactoryServiceBase(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

        public Task<byte[]> DefaultAdminRoleQueryAsync(DefaultAdminRoleFunction defaultAdminRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(defaultAdminRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> DefaultAdminRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> EventCreatorRoleQueryAsync(EventCreatorRoleFunction eventCreatorRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventCreatorRoleFunction, byte[]>(eventCreatorRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> EventCreatorRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventCreatorRoleFunction, byte[]>(null, blockParameter);
        }

        public virtual Task<string> AcceptDefaultAdminTransferRequestAsync(AcceptDefaultAdminTransferFunction acceptDefaultAdminTransferFunction)
        {
             return ContractHandler.SendRequestAsync(acceptDefaultAdminTransferFunction);
        }

        public virtual Task<string> AcceptDefaultAdminTransferRequestAsync()
        {
             return ContractHandler.SendRequestAsync<AcceptDefaultAdminTransferFunction>();
        }

        public virtual Task<TransactionReceipt> AcceptDefaultAdminTransferRequestAndWaitForReceiptAsync(AcceptDefaultAdminTransferFunction acceptDefaultAdminTransferFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(acceptDefaultAdminTransferFunction, cancellationToken);
        }

        public virtual Task<TransactionReceipt> AcceptDefaultAdminTransferRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<AcceptDefaultAdminTransferFunction>(null, cancellationToken);
        }

        public virtual Task<string> BeginDefaultAdminTransferRequestAsync(BeginDefaultAdminTransferFunction beginDefaultAdminTransferFunction)
        {
             return ContractHandler.SendRequestAsync(beginDefaultAdminTransferFunction);
        }

        public virtual Task<TransactionReceipt> BeginDefaultAdminTransferRequestAndWaitForReceiptAsync(BeginDefaultAdminTransferFunction beginDefaultAdminTransferFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(beginDefaultAdminTransferFunction, cancellationToken);
        }

        public virtual Task<string> BeginDefaultAdminTransferRequestAsync(string newAdmin)
        {
            var beginDefaultAdminTransferFunction = new BeginDefaultAdminTransferFunction();
                beginDefaultAdminTransferFunction.NewAdmin = newAdmin;
            
             return ContractHandler.SendRequestAsync(beginDefaultAdminTransferFunction);
        }

        public virtual Task<TransactionReceipt> BeginDefaultAdminTransferRequestAndWaitForReceiptAsync(string newAdmin, CancellationTokenSource cancellationToken = null)
        {
            var beginDefaultAdminTransferFunction = new BeginDefaultAdminTransferFunction();
                beginDefaultAdminTransferFunction.NewAdmin = newAdmin;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(beginDefaultAdminTransferFunction, cancellationToken);
        }

        public virtual Task<string> CancelDefaultAdminTransferRequestAsync(CancelDefaultAdminTransferFunction cancelDefaultAdminTransferFunction)
        {
             return ContractHandler.SendRequestAsync(cancelDefaultAdminTransferFunction);
        }

        public virtual Task<string> CancelDefaultAdminTransferRequestAsync()
        {
             return ContractHandler.SendRequestAsync<CancelDefaultAdminTransferFunction>();
        }

        public virtual Task<TransactionReceipt> CancelDefaultAdminTransferRequestAndWaitForReceiptAsync(CancelDefaultAdminTransferFunction cancelDefaultAdminTransferFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(cancelDefaultAdminTransferFunction, cancellationToken);
        }

        public virtual Task<TransactionReceipt> CancelDefaultAdminTransferRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<CancelDefaultAdminTransferFunction>(null, cancellationToken);
        }

        public virtual Task<string> ChangeDefaultAdminDelayRequestAsync(ChangeDefaultAdminDelayFunction changeDefaultAdminDelayFunction)
        {
             return ContractHandler.SendRequestAsync(changeDefaultAdminDelayFunction);
        }

        public virtual Task<TransactionReceipt> ChangeDefaultAdminDelayRequestAndWaitForReceiptAsync(ChangeDefaultAdminDelayFunction changeDefaultAdminDelayFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(changeDefaultAdminDelayFunction, cancellationToken);
        }

        public virtual Task<string> ChangeDefaultAdminDelayRequestAsync(ulong newDelay)
        {
            var changeDefaultAdminDelayFunction = new ChangeDefaultAdminDelayFunction();
                changeDefaultAdminDelayFunction.NewDelay = newDelay;
            
             return ContractHandler.SendRequestAsync(changeDefaultAdminDelayFunction);
        }

        public virtual Task<TransactionReceipt> ChangeDefaultAdminDelayRequestAndWaitForReceiptAsync(ulong newDelay, CancellationTokenSource cancellationToken = null)
        {
            var changeDefaultAdminDelayFunction = new ChangeDefaultAdminDelayFunction();
                changeDefaultAdminDelayFunction.NewDelay = newDelay;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(changeDefaultAdminDelayFunction, cancellationToken);
        }

        public virtual Task<string> CreateEventRequestAsync(CreateEventFunction createEventFunction)
        {
             return ContractHandler.SendRequestAsync(createEventFunction);
        }

        public virtual Task<TransactionReceipt> CreateEventRequestAndWaitForReceiptAsync(CreateEventFunction createEventFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createEventFunction, cancellationToken);
        }

        public virtual Task<string> CreateEventRequestAsync(CreateEventParams p)
        {
            var createEventFunction = new CreateEventFunction();
                createEventFunction.P = p;
            
             return ContractHandler.SendRequestAsync(createEventFunction);
        }

        public virtual Task<TransactionReceipt> CreateEventRequestAndWaitForReceiptAsync(CreateEventParams p, CancellationTokenSource cancellationToken = null)
        {
            var createEventFunction = new CreateEventFunction();
                createEventFunction.P = p;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createEventFunction, cancellationToken);
        }

        public Task<string> DefaultAdminQueryAsync(DefaultAdminFunction defaultAdminFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminFunction, string>(defaultAdminFunction, blockParameter);
        }

        
        public virtual Task<string> DefaultAdminQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminFunction, string>(null, blockParameter);
        }

        public Task<ulong> DefaultAdminDelayQueryAsync(DefaultAdminDelayFunction defaultAdminDelayFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminDelayFunction, ulong>(defaultAdminDelayFunction, blockParameter);
        }

        
        public virtual Task<ulong> DefaultAdminDelayQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminDelayFunction, ulong>(null, blockParameter);
        }

        public Task<ulong> DefaultAdminDelayIncreaseWaitQueryAsync(DefaultAdminDelayIncreaseWaitFunction defaultAdminDelayIncreaseWaitFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminDelayIncreaseWaitFunction, ulong>(defaultAdminDelayIncreaseWaitFunction, blockParameter);
        }

        
        public virtual Task<ulong> DefaultAdminDelayIncreaseWaitQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminDelayIncreaseWaitFunction, ulong>(null, blockParameter);
        }

        public Task<string> EscrowQueryAsync(EscrowFunction escrowFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EscrowFunction, string>(escrowFunction, blockParameter);
        }

        
        public virtual Task<string> EscrowQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EscrowFunction, string>(null, blockParameter);
        }

        public Task<BigInteger> EventCountQueryAsync(EventCountFunction eventCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventCountFunction, BigInteger>(eventCountFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> EventCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<byte[]> GetRoleAdminQueryAsync(GetRoleAdminFunction getRoleAdminFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetRoleAdminFunction, byte[]>(getRoleAdminFunction, blockParameter);
        }

        
        public virtual Task<byte[]> GetRoleAdminQueryAsync(byte[] role, BlockParameter blockParameter = null)
        {
            var getRoleAdminFunction = new GetRoleAdminFunction();
                getRoleAdminFunction.Role = role;
            
            return ContractHandler.QueryAsync<GetRoleAdminFunction, byte[]>(getRoleAdminFunction, blockParameter);
        }

        public virtual Task<string> GrantRoleRequestAsync(GrantRoleFunction grantRoleFunction)
        {
             return ContractHandler.SendRequestAsync(grantRoleFunction);
        }

        public virtual Task<TransactionReceipt> GrantRoleRequestAndWaitForReceiptAsync(GrantRoleFunction grantRoleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(grantRoleFunction, cancellationToken);
        }

        public virtual Task<string> GrantRoleRequestAsync(byte[] role, string account)
        {
            var grantRoleFunction = new GrantRoleFunction();
                grantRoleFunction.Role = role;
                grantRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAsync(grantRoleFunction);
        }

        public virtual Task<TransactionReceipt> GrantRoleRequestAndWaitForReceiptAsync(byte[] role, string account, CancellationTokenSource cancellationToken = null)
        {
            var grantRoleFunction = new GrantRoleFunction();
                grantRoleFunction.Role = role;
                grantRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(grantRoleFunction, cancellationToken);
        }

        public Task<bool> HasRoleQueryAsync(HasRoleFunction hasRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<HasRoleFunction, bool>(hasRoleFunction, blockParameter);
        }

        
        public virtual Task<bool> HasRoleQueryAsync(byte[] role, string account, BlockParameter blockParameter = null)
        {
            var hasRoleFunction = new HasRoleFunction();
                hasRoleFunction.Role = role;
                hasRoleFunction.Account = account;
            
            return ContractHandler.QueryAsync<HasRoleFunction, bool>(hasRoleFunction, blockParameter);
        }

        public Task<string> ImplementationQueryAsync(ImplementationFunction implementationFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ImplementationFunction, string>(implementationFunction, blockParameter);
        }

        
        public virtual Task<string> ImplementationQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ImplementationFunction, string>(null, blockParameter);
        }

        public Task<string> OwnerQueryAsync(OwnerFunction ownerFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(ownerFunction, blockParameter);
        }

        
        public virtual Task<string> OwnerQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(null, blockParameter);
        }

        public virtual Task<PendingDefaultAdminOutputDTO> PendingDefaultAdminQueryAsync(PendingDefaultAdminFunction pendingDefaultAdminFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<PendingDefaultAdminFunction, PendingDefaultAdminOutputDTO>(pendingDefaultAdminFunction, blockParameter);
        }

        public virtual Task<PendingDefaultAdminOutputDTO> PendingDefaultAdminQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<PendingDefaultAdminFunction, PendingDefaultAdminOutputDTO>(null, blockParameter);
        }

        public virtual Task<PendingDefaultAdminDelayOutputDTO> PendingDefaultAdminDelayQueryAsync(PendingDefaultAdminDelayFunction pendingDefaultAdminDelayFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<PendingDefaultAdminDelayFunction, PendingDefaultAdminDelayOutputDTO>(pendingDefaultAdminDelayFunction, blockParameter);
        }

        public virtual Task<PendingDefaultAdminDelayOutputDTO> PendingDefaultAdminDelayQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<PendingDefaultAdminDelayFunction, PendingDefaultAdminDelayOutputDTO>(null, blockParameter);
        }

        public virtual Task<PlatformOutputDTO> PlatformQueryAsync(PlatformFunction platformFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<PlatformFunction, PlatformOutputDTO>(platformFunction, blockParameter);
        }

        public virtual Task<PlatformOutputDTO> PlatformQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<PlatformFunction, PlatformOutputDTO>(null, blockParameter);
        }

        public Task<string> PredictTicketAddressQueryAsync(PredictTicketAddressFunction predictTicketAddressFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PredictTicketAddressFunction, string>(predictTicketAddressFunction, blockParameter);
        }

        
        public virtual Task<string> PredictTicketAddressQueryAsync(byte[] eventId, BlockParameter blockParameter = null)
        {
            var predictTicketAddressFunction = new PredictTicketAddressFunction();
                predictTicketAddressFunction.EventId = eventId;
            
            return ContractHandler.QueryAsync<PredictTicketAddressFunction, string>(predictTicketAddressFunction, blockParameter);
        }

        public Task<string> RendererQueryAsync(RendererFunction rendererFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RendererFunction, string>(rendererFunction, blockParameter);
        }

        
        public virtual Task<string> RendererQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RendererFunction, string>(null, blockParameter);
        }

        public virtual Task<string> RenounceRoleRequestAsync(RenounceRoleFunction renounceRoleFunction)
        {
             return ContractHandler.SendRequestAsync(renounceRoleFunction);
        }

        public virtual Task<TransactionReceipt> RenounceRoleRequestAndWaitForReceiptAsync(RenounceRoleFunction renounceRoleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(renounceRoleFunction, cancellationToken);
        }

        public virtual Task<string> RenounceRoleRequestAsync(byte[] role, string account)
        {
            var renounceRoleFunction = new RenounceRoleFunction();
                renounceRoleFunction.Role = role;
                renounceRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAsync(renounceRoleFunction);
        }

        public virtual Task<TransactionReceipt> RenounceRoleRequestAndWaitForReceiptAsync(byte[] role, string account, CancellationTokenSource cancellationToken = null)
        {
            var renounceRoleFunction = new RenounceRoleFunction();
                renounceRoleFunction.Role = role;
                renounceRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(renounceRoleFunction, cancellationToken);
        }

        public Task<string> ResaleQueryAsync(ResaleFunction resaleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ResaleFunction, string>(resaleFunction, blockParameter);
        }

        
        public virtual Task<string> ResaleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ResaleFunction, string>(null, blockParameter);
        }

        public virtual Task<string> RevokeRoleRequestAsync(RevokeRoleFunction revokeRoleFunction)
        {
             return ContractHandler.SendRequestAsync(revokeRoleFunction);
        }

        public virtual Task<TransactionReceipt> RevokeRoleRequestAndWaitForReceiptAsync(RevokeRoleFunction revokeRoleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(revokeRoleFunction, cancellationToken);
        }

        public virtual Task<string> RevokeRoleRequestAsync(byte[] role, string account)
        {
            var revokeRoleFunction = new RevokeRoleFunction();
                revokeRoleFunction.Role = role;
                revokeRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAsync(revokeRoleFunction);
        }

        public virtual Task<TransactionReceipt> RevokeRoleRequestAndWaitForReceiptAsync(byte[] role, string account, CancellationTokenSource cancellationToken = null)
        {
            var revokeRoleFunction = new RevokeRoleFunction();
                revokeRoleFunction.Role = role;
                revokeRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(revokeRoleFunction, cancellationToken);
        }

        public virtual Task<string> RollbackDefaultAdminDelayRequestAsync(RollbackDefaultAdminDelayFunction rollbackDefaultAdminDelayFunction)
        {
             return ContractHandler.SendRequestAsync(rollbackDefaultAdminDelayFunction);
        }

        public virtual Task<string> RollbackDefaultAdminDelayRequestAsync()
        {
             return ContractHandler.SendRequestAsync<RollbackDefaultAdminDelayFunction>();
        }

        public virtual Task<TransactionReceipt> RollbackDefaultAdminDelayRequestAndWaitForReceiptAsync(RollbackDefaultAdminDelayFunction rollbackDefaultAdminDelayFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(rollbackDefaultAdminDelayFunction, cancellationToken);
        }

        public virtual Task<TransactionReceipt> RollbackDefaultAdminDelayRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<RollbackDefaultAdminDelayFunction>(null, cancellationToken);
        }

        public virtual Task<string> SetPlatformConfigRequestAsync(SetPlatformConfigFunction setPlatformConfigFunction)
        {
             return ContractHandler.SendRequestAsync(setPlatformConfigFunction);
        }

        public virtual Task<TransactionReceipt> SetPlatformConfigRequestAndWaitForReceiptAsync(SetPlatformConfigFunction setPlatformConfigFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setPlatformConfigFunction, cancellationToken);
        }

        public virtual Task<string> SetPlatformConfigRequestAsync(PlatformConfig config)
        {
            var setPlatformConfigFunction = new SetPlatformConfigFunction();
                setPlatformConfigFunction.Config = config;
            
             return ContractHandler.SendRequestAsync(setPlatformConfigFunction);
        }

        public virtual Task<TransactionReceipt> SetPlatformConfigRequestAndWaitForReceiptAsync(PlatformConfig config, CancellationTokenSource cancellationToken = null)
        {
            var setPlatformConfigFunction = new SetPlatformConfigFunction();
                setPlatformConfigFunction.Config = config;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setPlatformConfigFunction, cancellationToken);
        }

        public Task<bool> SupportsInterfaceQueryAsync(SupportsInterfaceFunction supportsInterfaceFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<SupportsInterfaceFunction, bool>(supportsInterfaceFunction, blockParameter);
        }

        
        public virtual Task<bool> SupportsInterfaceQueryAsync(byte[] interfaceId, BlockParameter blockParameter = null)
        {
            var supportsInterfaceFunction = new SupportsInterfaceFunction();
                supportsInterfaceFunction.InterfaceId = interfaceId;
            
            return ContractHandler.QueryAsync<SupportsInterfaceFunction, bool>(supportsInterfaceFunction, blockParameter);
        }

        public Task<string> TicketOfQueryAsync(TicketOfFunction ticketOfFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TicketOfFunction, string>(ticketOfFunction, blockParameter);
        }

        
        public virtual Task<string> TicketOfQueryAsync(byte[] eventId, BlockParameter blockParameter = null)
        {
            var ticketOfFunction = new TicketOfFunction();
                ticketOfFunction.EventId = eventId;
            
            return ContractHandler.QueryAsync<TicketOfFunction, string>(ticketOfFunction, blockParameter);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(DefaultAdminRoleFunction),
                typeof(EventCreatorRoleFunction),
                typeof(AcceptDefaultAdminTransferFunction),
                typeof(BeginDefaultAdminTransferFunction),
                typeof(CancelDefaultAdminTransferFunction),
                typeof(ChangeDefaultAdminDelayFunction),
                typeof(CreateEventFunction),
                typeof(DefaultAdminFunction),
                typeof(DefaultAdminDelayFunction),
                typeof(DefaultAdminDelayIncreaseWaitFunction),
                typeof(EscrowFunction),
                typeof(EventCountFunction),
                typeof(GetRoleAdminFunction),
                typeof(GrantRoleFunction),
                typeof(HasRoleFunction),
                typeof(ImplementationFunction),
                typeof(OwnerFunction),
                typeof(PendingDefaultAdminFunction),
                typeof(PendingDefaultAdminDelayFunction),
                typeof(PlatformFunction),
                typeof(PredictTicketAddressFunction),
                typeof(RendererFunction),
                typeof(RenounceRoleFunction),
                typeof(ResaleFunction),
                typeof(RevokeRoleFunction),
                typeof(RollbackDefaultAdminDelayFunction),
                typeof(SetPlatformConfigFunction),
                typeof(SupportsInterfaceFunction),
                typeof(TicketOfFunction)
            };
        }

        public override List<Type> GetAllEventTypes()
        {
            return new List<Type>
            {
                typeof(DefaultAdminDelayChangeCanceledEventDTO),
                typeof(DefaultAdminDelayChangeScheduledEventDTO),
                typeof(DefaultAdminTransferCanceledEventDTO),
                typeof(DefaultAdminTransferScheduledEventDTO),
                typeof(EventCreatedEventDTO),
                typeof(PlatformConfigSetEventDTO),
                typeof(RoleAdminChangedEventDTO),
                typeof(RoleGrantedEventDTO),
                typeof(RoleRevokedEventDTO)
            };
        }

        public override List<Type> GetAllErrorTypes()
        {
            return new List<Type>
            {
                typeof(AccessControlBadConfirmationError),
                typeof(AccessControlEnforcedDefaultAdminDelayError),
                typeof(AccessControlEnforcedDefaultAdminRulesError),
                typeof(AccessControlInvalidDefaultAdminError),
                typeof(AccessControlUnauthorizedAccountError),
                typeof(FailedDeploymentError),
                typeof(InsufficientBalanceError),
                typeof(SafeCastOverflowedUintDowncastError),
                typeof(ZeroAddressError)
            };
        }
    }
}
