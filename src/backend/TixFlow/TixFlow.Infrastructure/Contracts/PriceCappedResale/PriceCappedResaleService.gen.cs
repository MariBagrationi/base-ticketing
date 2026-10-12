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
using TixFlow.Infrastructure.Contracts.PriceCappedResale.ContractDefinition;

namespace TixFlow.Infrastructure.Contracts.PriceCappedResale
{
    public partial class PriceCappedResaleService: PriceCappedResaleServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, PriceCappedResaleDeployment priceCappedResaleDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<PriceCappedResaleDeployment>().SendRequestAndWaitForReceiptAsync(priceCappedResaleDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, PriceCappedResaleDeployment priceCappedResaleDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<PriceCappedResaleDeployment>().SendRequestAsync(priceCappedResaleDeployment);
        }

        public static async Task<PriceCappedResaleService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, PriceCappedResaleDeployment priceCappedResaleDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, priceCappedResaleDeployment, cancellationTokenSource);
            return new PriceCappedResaleService(web3, receipt.ContractAddress);
        }

        public PriceCappedResaleService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

    }


    public partial class PriceCappedResaleServiceBase: ContractWeb3ServiceBase
    {

        public PriceCappedResaleServiceBase(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

        public Task<ushort> BpsDenominatorQueryAsync(BpsDenominatorFunction bpsDenominatorFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BpsDenominatorFunction, ushort>(bpsDenominatorFunction, blockParameter);
        }

        
        public virtual Task<ushort> BpsDenominatorQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BpsDenominatorFunction, ushort>(null, blockParameter);
        }

        public Task<byte[]> DefaultAdminRoleQueryAsync(DefaultAdminRoleFunction defaultAdminRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(defaultAdminRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> DefaultAdminRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(null, blockParameter);
        }

        public Task<ushort> MaxMarkupBpsQueryAsync(MaxMarkupBpsFunction maxMarkupBpsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MaxMarkupBpsFunction, ushort>(maxMarkupBpsFunction, blockParameter);
        }

        
        public virtual Task<ushort> MaxMarkupBpsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MaxMarkupBpsFunction, ushort>(null, blockParameter);
        }

        public Task<ushort> MaxProtocolFeeBpsQueryAsync(MaxProtocolFeeBpsFunction maxProtocolFeeBpsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MaxProtocolFeeBpsFunction, ushort>(maxProtocolFeeBpsFunction, blockParameter);
        }

        
        public virtual Task<ushort> MaxProtocolFeeBpsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MaxProtocolFeeBpsFunction, ushort>(null, blockParameter);
        }

        public Task<byte[]> RegistrarRoleQueryAsync(RegistrarRoleFunction registrarRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RegistrarRoleFunction, byte[]>(registrarRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> RegistrarRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RegistrarRoleFunction, byte[]>(null, blockParameter);
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

        public virtual Task<string> BuyRequestAsync(BuyFunction buyFunction)
        {
             return ContractHandler.SendRequestAsync(buyFunction);
        }

        public virtual Task<TransactionReceipt> BuyRequestAndWaitForReceiptAsync(BuyFunction buyFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(buyFunction, cancellationToken);
        }

        public virtual Task<string> BuyRequestAsync(BigInteger listingId)
        {
            var buyFunction = new BuyFunction();
                buyFunction.ListingId = listingId;
            
             return ContractHandler.SendRequestAsync(buyFunction);
        }

        public virtual Task<TransactionReceipt> BuyRequestAndWaitForReceiptAsync(BigInteger listingId, CancellationTokenSource cancellationToken = null)
        {
            var buyFunction = new BuyFunction();
                buyFunction.ListingId = listingId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(buyFunction, cancellationToken);
        }

        public virtual Task<string> CancelRequestAsync(CancelFunction cancelFunction)
        {
             return ContractHandler.SendRequestAsync(cancelFunction);
        }

        public virtual Task<TransactionReceipt> CancelRequestAndWaitForReceiptAsync(CancelFunction cancelFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(cancelFunction, cancellationToken);
        }

        public virtual Task<string> CancelRequestAsync(BigInteger listingId)
        {
            var cancelFunction = new CancelFunction();
                cancelFunction.ListingId = listingId;
            
             return ContractHandler.SendRequestAsync(cancelFunction);
        }

        public virtual Task<TransactionReceipt> CancelRequestAndWaitForReceiptAsync(BigInteger listingId, CancellationTokenSource cancellationToken = null)
        {
            var cancelFunction = new CancelFunction();
                cancelFunction.ListingId = listingId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(cancelFunction, cancellationToken);
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

        public Task<string> FeeRecipientQueryAsync(FeeRecipientFunction feeRecipientFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<FeeRecipientFunction, string>(feeRecipientFunction, blockParameter);
        }

        
        public virtual Task<string> FeeRecipientQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<FeeRecipientFunction, string>(null, blockParameter);
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

        public Task<bool> IsRegisteredTicketQueryAsync(IsRegisteredTicketFunction isRegisteredTicketFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<IsRegisteredTicketFunction, bool>(isRegisteredTicketFunction, blockParameter);
        }

        
        public virtual Task<bool> IsRegisteredTicketQueryAsync(string ticket, BlockParameter blockParameter = null)
        {
            var isRegisteredTicketFunction = new IsRegisteredTicketFunction();
                isRegisteredTicketFunction.Ticket = ticket;
            
            return ContractHandler.QueryAsync<IsRegisteredTicketFunction, bool>(isRegisteredTicketFunction, blockParameter);
        }

        public virtual Task<string> ListRequestAsync(ListFunction listFunction)
        {
             return ContractHandler.SendRequestAsync(listFunction);
        }

        public virtual Task<TransactionReceipt> ListRequestAndWaitForReceiptAsync(ListFunction listFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(listFunction, cancellationToken);
        }

        public virtual Task<string> ListRequestAsync(string ticket, BigInteger tokenId, BigInteger price)
        {
            var listFunction = new ListFunction();
                listFunction.Ticket = ticket;
                listFunction.TokenId = tokenId;
                listFunction.Price = price;
            
             return ContractHandler.SendRequestAsync(listFunction);
        }

        public virtual Task<TransactionReceipt> ListRequestAndWaitForReceiptAsync(string ticket, BigInteger tokenId, BigInteger price, CancellationTokenSource cancellationToken = null)
        {
            var listFunction = new ListFunction();
                listFunction.Ticket = ticket;
                listFunction.TokenId = tokenId;
                listFunction.Price = price;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(listFunction, cancellationToken);
        }

        public virtual Task<ListingsOutputDTO> ListingsQueryAsync(ListingsFunction listingsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<ListingsFunction, ListingsOutputDTO>(listingsFunction, blockParameter);
        }

        public virtual Task<ListingsOutputDTO> ListingsQueryAsync(BigInteger listingId, BlockParameter blockParameter = null)
        {
            var listingsFunction = new ListingsFunction();
                listingsFunction.ListingId = listingId;
            
            return ContractHandler.QueryDeserializingToObjectAsync<ListingsFunction, ListingsOutputDTO>(listingsFunction, blockParameter);
        }

        public Task<ushort> MarkupBpsQueryAsync(MarkupBpsFunction markupBpsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MarkupBpsFunction, ushort>(markupBpsFunction, blockParameter);
        }

        
        public virtual Task<ushort> MarkupBpsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MarkupBpsFunction, ushort>(null, blockParameter);
        }

        public Task<BigInteger> MaxResalePriceQueryAsync(MaxResalePriceFunction maxResalePriceFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MaxResalePriceFunction, BigInteger>(maxResalePriceFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> MaxResalePriceQueryAsync(string ticket, BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var maxResalePriceFunction = new MaxResalePriceFunction();
                maxResalePriceFunction.Ticket = ticket;
                maxResalePriceFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<MaxResalePriceFunction, BigInteger>(maxResalePriceFunction, blockParameter);
        }

        public Task<BigInteger> NextListingIdQueryAsync(NextListingIdFunction nextListingIdFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NextListingIdFunction, BigInteger>(nextListingIdFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> NextListingIdQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NextListingIdFunction, BigInteger>(null, blockParameter);
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

        public Task<BigInteger> PendingWithdrawalsQueryAsync(PendingWithdrawalsFunction pendingWithdrawalsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PendingWithdrawalsFunction, BigInteger>(pendingWithdrawalsFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> PendingWithdrawalsQueryAsync(string account, BlockParameter blockParameter = null)
        {
            var pendingWithdrawalsFunction = new PendingWithdrawalsFunction();
                pendingWithdrawalsFunction.Account = account;
            
            return ContractHandler.QueryAsync<PendingWithdrawalsFunction, BigInteger>(pendingWithdrawalsFunction, blockParameter);
        }

        public Task<ushort> ProtocolFeeBpsQueryAsync(ProtocolFeeBpsFunction protocolFeeBpsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ProtocolFeeBpsFunction, ushort>(protocolFeeBpsFunction, blockParameter);
        }

        
        public virtual Task<ushort> ProtocolFeeBpsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ProtocolFeeBpsFunction, ushort>(null, blockParameter);
        }

        public virtual Task<string> RegisterTicketRequestAsync(RegisterTicketFunction registerTicketFunction)
        {
             return ContractHandler.SendRequestAsync(registerTicketFunction);
        }

        public virtual Task<TransactionReceipt> RegisterTicketRequestAndWaitForReceiptAsync(RegisterTicketFunction registerTicketFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(registerTicketFunction, cancellationToken);
        }

        public virtual Task<string> RegisterTicketRequestAsync(string ticket)
        {
            var registerTicketFunction = new RegisterTicketFunction();
                registerTicketFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAsync(registerTicketFunction);
        }

        public virtual Task<TransactionReceipt> RegisterTicketRequestAndWaitForReceiptAsync(string ticket, CancellationTokenSource cancellationToken = null)
        {
            var registerTicketFunction = new RegisterTicketFunction();
                registerTicketFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(registerTicketFunction, cancellationToken);
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

        public virtual Task<string> SetMarkupBpsRequestAsync(SetMarkupBpsFunction setMarkupBpsFunction)
        {
             return ContractHandler.SendRequestAsync(setMarkupBpsFunction);
        }

        public virtual Task<TransactionReceipt> SetMarkupBpsRequestAndWaitForReceiptAsync(SetMarkupBpsFunction setMarkupBpsFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setMarkupBpsFunction, cancellationToken);
        }

        public virtual Task<string> SetMarkupBpsRequestAsync(ushort newMarkupBps)
        {
            var setMarkupBpsFunction = new SetMarkupBpsFunction();
                setMarkupBpsFunction.NewMarkupBps = newMarkupBps;
            
             return ContractHandler.SendRequestAsync(setMarkupBpsFunction);
        }

        public virtual Task<TransactionReceipt> SetMarkupBpsRequestAndWaitForReceiptAsync(ushort newMarkupBps, CancellationTokenSource cancellationToken = null)
        {
            var setMarkupBpsFunction = new SetMarkupBpsFunction();
                setMarkupBpsFunction.NewMarkupBps = newMarkupBps;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setMarkupBpsFunction, cancellationToken);
        }

        public virtual Task<string> SetProtocolFeeRequestAsync(SetProtocolFeeFunction setProtocolFeeFunction)
        {
             return ContractHandler.SendRequestAsync(setProtocolFeeFunction);
        }

        public virtual Task<TransactionReceipt> SetProtocolFeeRequestAndWaitForReceiptAsync(SetProtocolFeeFunction setProtocolFeeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setProtocolFeeFunction, cancellationToken);
        }

        public virtual Task<string> SetProtocolFeeRequestAsync(ushort newFeeBps, string newRecipient)
        {
            var setProtocolFeeFunction = new SetProtocolFeeFunction();
                setProtocolFeeFunction.NewFeeBps = newFeeBps;
                setProtocolFeeFunction.NewRecipient = newRecipient;
            
             return ContractHandler.SendRequestAsync(setProtocolFeeFunction);
        }

        public virtual Task<TransactionReceipt> SetProtocolFeeRequestAndWaitForReceiptAsync(ushort newFeeBps, string newRecipient, CancellationTokenSource cancellationToken = null)
        {
            var setProtocolFeeFunction = new SetProtocolFeeFunction();
                setProtocolFeeFunction.NewFeeBps = newFeeBps;
                setProtocolFeeFunction.NewRecipient = newRecipient;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setProtocolFeeFunction, cancellationToken);
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

        public Task<string> UsdcQueryAsync(UsdcFunction usdcFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UsdcFunction, string>(usdcFunction, blockParameter);
        }

        
        public virtual Task<string> UsdcQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UsdcFunction, string>(null, blockParameter);
        }

        public virtual Task<string> WithdrawRequestAsync(WithdrawFunction withdrawFunction)
        {
             return ContractHandler.SendRequestAsync(withdrawFunction);
        }

        public virtual Task<string> WithdrawRequestAsync()
        {
             return ContractHandler.SendRequestAsync<WithdrawFunction>();
        }

        public virtual Task<TransactionReceipt> WithdrawRequestAndWaitForReceiptAsync(WithdrawFunction withdrawFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(withdrawFunction, cancellationToken);
        }

        public virtual Task<TransactionReceipt> WithdrawRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<WithdrawFunction>(null, cancellationToken);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(BpsDenominatorFunction),
                typeof(DefaultAdminRoleFunction),
                typeof(MaxMarkupBpsFunction),
                typeof(MaxProtocolFeeBpsFunction),
                typeof(RegistrarRoleFunction),
                typeof(AcceptDefaultAdminTransferFunction),
                typeof(BeginDefaultAdminTransferFunction),
                typeof(BuyFunction),
                typeof(CancelFunction),
                typeof(CancelDefaultAdminTransferFunction),
                typeof(ChangeDefaultAdminDelayFunction),
                typeof(DefaultAdminFunction),
                typeof(DefaultAdminDelayFunction),
                typeof(DefaultAdminDelayIncreaseWaitFunction),
                typeof(EscrowFunction),
                typeof(FeeRecipientFunction),
                typeof(GetRoleAdminFunction),
                typeof(GrantRoleFunction),
                typeof(HasRoleFunction),
                typeof(IsRegisteredTicketFunction),
                typeof(ListFunction),
                typeof(ListingsFunction),
                typeof(MarkupBpsFunction),
                typeof(MaxResalePriceFunction),
                typeof(NextListingIdFunction),
                typeof(OwnerFunction),
                typeof(PendingDefaultAdminFunction),
                typeof(PendingDefaultAdminDelayFunction),
                typeof(PendingWithdrawalsFunction),
                typeof(ProtocolFeeBpsFunction),
                typeof(RegisterTicketFunction),
                typeof(RenounceRoleFunction),
                typeof(RevokeRoleFunction),
                typeof(RollbackDefaultAdminDelayFunction),
                typeof(SetMarkupBpsFunction),
                typeof(SetProtocolFeeFunction),
                typeof(SupportsInterfaceFunction),
                typeof(UsdcFunction),
                typeof(WithdrawFunction)
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
                typeof(ListedEventDTO),
                typeof(ListingCancelledEventDTO),
                typeof(MarkupSetEventDTO),
                typeof(ProtocolFeeSetEventDTO),
                typeof(RoleAdminChangedEventDTO),
                typeof(RoleGrantedEventDTO),
                typeof(RoleRevokedEventDTO),
                typeof(SoldEventDTO),
                typeof(TicketRegisteredEventDTO),
                typeof(WithdrawnEventDTO)
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
                typeof(AlreadyRegisteredError),
                typeof(InvalidBpsError),
                typeof(ListingNotActiveError),
                typeof(NotRegisteredError),
                typeof(NotSellerError),
                typeof(NotTicketOwnerError),
                typeof(NothingToWithdrawError),
                typeof(PriceAboveCapError),
                typeof(ReentrancyGuardReentrantCallError),
                typeof(ResaleClosedError),
                typeof(SafeCastOverflowedUintDowncastError),
                typeof(SafeERC20FailedOperationError),
                typeof(TicketRedeemedError),
                typeof(ZeroAddressError),
                typeof(ZeroPriceError)
            };
        }
    }
}
