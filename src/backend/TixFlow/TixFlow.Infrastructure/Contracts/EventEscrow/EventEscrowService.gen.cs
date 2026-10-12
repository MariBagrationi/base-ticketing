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
using TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition;

namespace TixFlow.Infrastructure.Contracts.EventEscrow
{
    public partial class EventEscrowService: EventEscrowServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, EventEscrowDeployment eventEscrowDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<EventEscrowDeployment>().SendRequestAndWaitForReceiptAsync(eventEscrowDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, EventEscrowDeployment eventEscrowDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<EventEscrowDeployment>().SendRequestAsync(eventEscrowDeployment);
        }

        public static async Task<EventEscrowService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, EventEscrowDeployment eventEscrowDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, eventEscrowDeployment, cancellationTokenSource);
            return new EventEscrowService(web3, receipt.ContractAddress);
        }

        public EventEscrowService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

    }


    public partial class EventEscrowServiceBase: ContractWeb3ServiceBase
    {

        public EventEscrowServiceBase(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

        public Task<byte[]> CancellerRoleQueryAsync(CancellerRoleFunction cancellerRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CancellerRoleFunction, byte[]>(cancellerRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> CancellerRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CancellerRoleFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> DefaultAdminRoleQueryAsync(DefaultAdminRoleFunction defaultAdminRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(defaultAdminRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> DefaultAdminRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(null, blockParameter);
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

        public virtual Task<string> CancelEventRequestAsync(CancelEventFunction cancelEventFunction)
        {
             return ContractHandler.SendRequestAsync(cancelEventFunction);
        }

        public virtual Task<TransactionReceipt> CancelEventRequestAndWaitForReceiptAsync(CancelEventFunction cancelEventFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(cancelEventFunction, cancellationToken);
        }

        public virtual Task<string> CancelEventRequestAsync(string ticket)
        {
            var cancelEventFunction = new CancelEventFunction();
                cancelEventFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAsync(cancelEventFunction);
        }

        public virtual Task<TransactionReceipt> CancelEventRequestAndWaitForReceiptAsync(string ticket, CancellationTokenSource cancellationToken = null)
        {
            var cancelEventFunction = new CancelEventFunction();
                cancelEventFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(cancelEventFunction, cancellationToken);
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

        public virtual Task<string> ClaimRefundRequestAsync(ClaimRefundFunction claimRefundFunction)
        {
             return ContractHandler.SendRequestAsync(claimRefundFunction);
        }

        public virtual Task<TransactionReceipt> ClaimRefundRequestAndWaitForReceiptAsync(ClaimRefundFunction claimRefundFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(claimRefundFunction, cancellationToken);
        }

        public virtual Task<string> ClaimRefundRequestAsync(string ticket, BigInteger tokenId)
        {
            var claimRefundFunction = new ClaimRefundFunction();
                claimRefundFunction.Ticket = ticket;
                claimRefundFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAsync(claimRefundFunction);
        }

        public virtual Task<TransactionReceipt> ClaimRefundRequestAndWaitForReceiptAsync(string ticket, BigInteger tokenId, CancellationTokenSource cancellationToken = null)
        {
            var claimRefundFunction = new ClaimRefundFunction();
                claimRefundFunction.Ticket = ticket;
                claimRefundFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(claimRefundFunction, cancellationToken);
        }

        public virtual Task<string> CollectPaymentRequestAsync(CollectPaymentFunction collectPaymentFunction)
        {
             return ContractHandler.SendRequestAsync(collectPaymentFunction);
        }

        public virtual Task<TransactionReceipt> CollectPaymentRequestAndWaitForReceiptAsync(CollectPaymentFunction collectPaymentFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(collectPaymentFunction, cancellationToken);
        }

        public virtual Task<string> CollectPaymentRequestAsync(string payer, string buyer, BigInteger firstTokenId, BigInteger quantity, BigInteger unitPrice)
        {
            var collectPaymentFunction = new CollectPaymentFunction();
                collectPaymentFunction.Payer = payer;
                collectPaymentFunction.Buyer = buyer;
                collectPaymentFunction.FirstTokenId = firstTokenId;
                collectPaymentFunction.Quantity = quantity;
                collectPaymentFunction.UnitPrice = unitPrice;
            
             return ContractHandler.SendRequestAsync(collectPaymentFunction);
        }

        public virtual Task<TransactionReceipt> CollectPaymentRequestAndWaitForReceiptAsync(string payer, string buyer, BigInteger firstTokenId, BigInteger quantity, BigInteger unitPrice, CancellationTokenSource cancellationToken = null)
        {
            var collectPaymentFunction = new CollectPaymentFunction();
                collectPaymentFunction.Payer = payer;
                collectPaymentFunction.Buyer = buyer;
                collectPaymentFunction.FirstTokenId = firstTokenId;
                collectPaymentFunction.Quantity = quantity;
                collectPaymentFunction.UnitPrice = unitPrice;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(collectPaymentFunction, cancellationToken);
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

        public virtual Task<EventAccountOutputDTO> EventAccountQueryAsync(EventAccountFunction eventAccountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<EventAccountFunction, EventAccountOutputDTO>(eventAccountFunction, blockParameter);
        }

        public virtual Task<EventAccountOutputDTO> EventAccountQueryAsync(string ticket, BlockParameter blockParameter = null)
        {
            var eventAccountFunction = new EventAccountFunction();
                eventAccountFunction.Ticket = ticket;
            
            return ContractHandler.QueryDeserializingToObjectAsync<EventAccountFunction, EventAccountOutputDTO>(eventAccountFunction, blockParameter);
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

        public Task<bool> IsCancelledQueryAsync(IsCancelledFunction isCancelledFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<IsCancelledFunction, bool>(isCancelledFunction, blockParameter);
        }

        
        public virtual Task<bool> IsCancelledQueryAsync(string ticket, BlockParameter blockParameter = null)
        {
            var isCancelledFunction = new IsCancelledFunction();
                isCancelledFunction.Ticket = ticket;
            
            return ContractHandler.QueryAsync<IsCancelledFunction, bool>(isCancelledFunction, blockParameter);
        }

        public Task<string> OwnerQueryAsync(OwnerFunction ownerFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(ownerFunction, blockParameter);
        }

        
        public virtual Task<string> OwnerQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(null, blockParameter);
        }

        public Task<BigInteger> PaidForQueryAsync(PaidForFunction paidForFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PaidForFunction, BigInteger>(paidForFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> PaidForQueryAsync(string ticket, BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var paidForFunction = new PaidForFunction();
                paidForFunction.Ticket = ticket;
                paidForFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<PaidForFunction, BigInteger>(paidForFunction, blockParameter);
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

        public virtual Task<string> PurchaseWithAuthorizationRequestAsync(PurchaseWithAuthorizationFunction purchaseWithAuthorizationFunction)
        {
             return ContractHandler.SendRequestAsync(purchaseWithAuthorizationFunction);
        }

        public virtual Task<TransactionReceipt> PurchaseWithAuthorizationRequestAndWaitForReceiptAsync(PurchaseWithAuthorizationFunction purchaseWithAuthorizationFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(purchaseWithAuthorizationFunction, cancellationToken);
        }

        public virtual Task<string> PurchaseWithAuthorizationRequestAsync(string ticket, MintVoucher voucher, byte[] voucherSignature, PaymentAuthorization auth)
        {
            var purchaseWithAuthorizationFunction = new PurchaseWithAuthorizationFunction();
                purchaseWithAuthorizationFunction.Ticket = ticket;
                purchaseWithAuthorizationFunction.Voucher = voucher;
                purchaseWithAuthorizationFunction.VoucherSignature = voucherSignature;
                purchaseWithAuthorizationFunction.Auth = auth;
            
             return ContractHandler.SendRequestAsync(purchaseWithAuthorizationFunction);
        }

        public virtual Task<TransactionReceipt> PurchaseWithAuthorizationRequestAndWaitForReceiptAsync(string ticket, MintVoucher voucher, byte[] voucherSignature, PaymentAuthorization auth, CancellationTokenSource cancellationToken = null)
        {
            var purchaseWithAuthorizationFunction = new PurchaseWithAuthorizationFunction();
                purchaseWithAuthorizationFunction.Ticket = ticket;
                purchaseWithAuthorizationFunction.Voucher = voucher;
                purchaseWithAuthorizationFunction.VoucherSignature = voucherSignature;
                purchaseWithAuthorizationFunction.Auth = auth;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(purchaseWithAuthorizationFunction, cancellationToken);
        }

        public virtual Task<string> PurchaseWithPermitRequestAsync(PurchaseWithPermitFunction purchaseWithPermitFunction)
        {
             return ContractHandler.SendRequestAsync(purchaseWithPermitFunction);
        }

        public virtual Task<TransactionReceipt> PurchaseWithPermitRequestAndWaitForReceiptAsync(PurchaseWithPermitFunction purchaseWithPermitFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(purchaseWithPermitFunction, cancellationToken);
        }

        public virtual Task<string> PurchaseWithPermitRequestAsync(string ticket, MintVoucher voucher, byte[] voucherSignature, PermitSignature permit)
        {
            var purchaseWithPermitFunction = new PurchaseWithPermitFunction();
                purchaseWithPermitFunction.Ticket = ticket;
                purchaseWithPermitFunction.Voucher = voucher;
                purchaseWithPermitFunction.VoucherSignature = voucherSignature;
                purchaseWithPermitFunction.Permit = permit;
            
             return ContractHandler.SendRequestAsync(purchaseWithPermitFunction);
        }

        public virtual Task<TransactionReceipt> PurchaseWithPermitRequestAndWaitForReceiptAsync(string ticket, MintVoucher voucher, byte[] voucherSignature, PermitSignature permit, CancellationTokenSource cancellationToken = null)
        {
            var purchaseWithPermitFunction = new PurchaseWithPermitFunction();
                purchaseWithPermitFunction.Ticket = ticket;
                purchaseWithPermitFunction.Voucher = voucher;
                purchaseWithPermitFunction.VoucherSignature = voucherSignature;
                purchaseWithPermitFunction.Permit = permit;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(purchaseWithPermitFunction, cancellationToken);
        }

        public virtual Task<string> RefundBatchRequestAsync(RefundBatchFunction refundBatchFunction)
        {
             return ContractHandler.SendRequestAsync(refundBatchFunction);
        }

        public virtual Task<TransactionReceipt> RefundBatchRequestAndWaitForReceiptAsync(RefundBatchFunction refundBatchFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(refundBatchFunction, cancellationToken);
        }

        public virtual Task<string> RefundBatchRequestAsync(string ticket, List<BigInteger> tokenIds)
        {
            var refundBatchFunction = new RefundBatchFunction();
                refundBatchFunction.Ticket = ticket;
                refundBatchFunction.TokenIds = tokenIds;
            
             return ContractHandler.SendRequestAsync(refundBatchFunction);
        }

        public virtual Task<TransactionReceipt> RefundBatchRequestAndWaitForReceiptAsync(string ticket, List<BigInteger> tokenIds, CancellationTokenSource cancellationToken = null)
        {
            var refundBatchFunction = new RefundBatchFunction();
                refundBatchFunction.Ticket = ticket;
                refundBatchFunction.TokenIds = tokenIds;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(refundBatchFunction, cancellationToken);
        }

        public virtual Task<string> RegisterEventRequestAsync(RegisterEventFunction registerEventFunction)
        {
             return ContractHandler.SendRequestAsync(registerEventFunction);
        }

        public virtual Task<TransactionReceipt> RegisterEventRequestAndWaitForReceiptAsync(RegisterEventFunction registerEventFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(registerEventFunction, cancellationToken);
        }

        public virtual Task<string> RegisterEventRequestAsync(string ticket)
        {
            var registerEventFunction = new RegisterEventFunction();
                registerEventFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAsync(registerEventFunction);
        }

        public virtual Task<TransactionReceipt> RegisterEventRequestAndWaitForReceiptAsync(string ticket, CancellationTokenSource cancellationToken = null)
        {
            var registerEventFunction = new RegisterEventFunction();
                registerEventFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(registerEventFunction, cancellationToken);
        }

        public virtual Task<string> ReleaseRequestAsync(ReleaseFunction releaseFunction)
        {
             return ContractHandler.SendRequestAsync(releaseFunction);
        }

        public virtual Task<TransactionReceipt> ReleaseRequestAndWaitForReceiptAsync(ReleaseFunction releaseFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(releaseFunction, cancellationToken);
        }

        public virtual Task<string> ReleaseRequestAsync(string ticket)
        {
            var releaseFunction = new ReleaseFunction();
                releaseFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAsync(releaseFunction);
        }

        public virtual Task<TransactionReceipt> ReleaseRequestAndWaitForReceiptAsync(string ticket, CancellationTokenSource cancellationToken = null)
        {
            var releaseFunction = new ReleaseFunction();
                releaseFunction.Ticket = ticket;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(releaseFunction, cancellationToken);
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

        public Task<BigInteger> TotalHeldQueryAsync(TotalHeldFunction totalHeldFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalHeldFunction, BigInteger>(totalHeldFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> TotalHeldQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalHeldFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> UsdcQueryAsync(UsdcFunction usdcFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UsdcFunction, string>(usdcFunction, blockParameter);
        }

        
        public virtual Task<string> UsdcQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UsdcFunction, string>(null, blockParameter);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(CancellerRoleFunction),
                typeof(DefaultAdminRoleFunction),
                typeof(RegistrarRoleFunction),
                typeof(AcceptDefaultAdminTransferFunction),
                typeof(BeginDefaultAdminTransferFunction),
                typeof(CancelDefaultAdminTransferFunction),
                typeof(CancelEventFunction),
                typeof(ChangeDefaultAdminDelayFunction),
                typeof(ClaimRefundFunction),
                typeof(CollectPaymentFunction),
                typeof(DefaultAdminFunction),
                typeof(DefaultAdminDelayFunction),
                typeof(DefaultAdminDelayIncreaseWaitFunction),
                typeof(EventAccountFunction),
                typeof(GetRoleAdminFunction),
                typeof(GrantRoleFunction),
                typeof(HasRoleFunction),
                typeof(IsCancelledFunction),
                typeof(OwnerFunction),
                typeof(PaidForFunction),
                typeof(PendingDefaultAdminFunction),
                typeof(PendingDefaultAdminDelayFunction),
                typeof(PurchaseWithAuthorizationFunction),
                typeof(PurchaseWithPermitFunction),
                typeof(RefundBatchFunction),
                typeof(RegisterEventFunction),
                typeof(ReleaseFunction),
                typeof(RenounceRoleFunction),
                typeof(RevokeRoleFunction),
                typeof(RollbackDefaultAdminDelayFunction),
                typeof(SupportsInterfaceFunction),
                typeof(TotalHeldFunction),
                typeof(UsdcFunction)
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
                typeof(EscrowReleasedEventDTO),
                typeof(EventCancelledEventDTO),
                typeof(EventRegisteredEventDTO),
                typeof(PaymentDepositedEventDTO),
                typeof(RefundedEventDTO),
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
                typeof(AlreadyRegisteredError),
                typeof(AlreadyReleasedError),
                typeof(EventIsCancelledError),
                typeof(EventNotCancelledError),
                typeof(EventNotEndedError),
                typeof(NotAuthorizedError),
                typeof(NotRegisteredError),
                typeof(NothingToRefundError),
                typeof(NothingToReleaseError),
                typeof(PrepaidMismatchError),
                typeof(ReentrancyGuardReentrantCallError),
                typeof(SafeCastOverflowedUintDowncastError),
                typeof(SafeERC20FailedOperationError),
                typeof(TicketListedError),
                typeof(ZeroAddressError)
            };
        }
    }
}
