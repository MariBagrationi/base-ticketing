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
using TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition;

namespace TixFlow.Infrastructure.Contracts.EventTicket
{
    public partial class EventTicketService: EventTicketServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, EventTicketDeployment eventTicketDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<EventTicketDeployment>().SendRequestAndWaitForReceiptAsync(eventTicketDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, EventTicketDeployment eventTicketDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<EventTicketDeployment>().SendRequestAsync(eventTicketDeployment);
        }

        public static async Task<EventTicketService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, EventTicketDeployment eventTicketDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, eventTicketDeployment, cancellationTokenSource);
            return new EventTicketService(web3, receipt.ContractAddress);
        }

        public EventTicketService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

    }


    public partial class EventTicketServiceBase: ContractWeb3ServiceBase
    {

        public EventTicketServiceBase(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
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

        public Task<byte[]> MinterRoleQueryAsync(MinterRoleFunction minterRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MinterRoleFunction, byte[]>(minterRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> MinterRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MinterRoleFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> MintVoucherTypehashQueryAsync(MintVoucherTypehashFunction mintVoucherTypehashFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MintVoucherTypehashFunction, byte[]>(mintVoucherTypehashFunction, blockParameter);
        }

        
        public virtual Task<byte[]> MintVoucherTypehashQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MintVoucherTypehashFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> PauserRoleQueryAsync(PauserRoleFunction pauserRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PauserRoleFunction, byte[]>(pauserRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> PauserRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PauserRoleFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> RedeemerRoleQueryAsync(RedeemerRoleFunction redeemerRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RedeemerRoleFunction, byte[]>(redeemerRoleFunction, blockParameter);
        }

        
        public virtual Task<byte[]> RedeemerRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RedeemerRoleFunction, byte[]>(null, blockParameter);
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

        public virtual Task<string> ApproveRequestAsync(ApproveFunction approveFunction)
        {
             return ContractHandler.SendRequestAsync(approveFunction);
        }

        public virtual Task<TransactionReceipt> ApproveRequestAndWaitForReceiptAsync(ApproveFunction approveFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(approveFunction, cancellationToken);
        }

        public virtual Task<string> ApproveRequestAsync(string to, BigInteger tokenId)
        {
            var approveFunction = new ApproveFunction();
                approveFunction.To = to;
                approveFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAsync(approveFunction);
        }

        public virtual Task<TransactionReceipt> ApproveRequestAndWaitForReceiptAsync(string to, BigInteger tokenId, CancellationTokenSource cancellationToken = null)
        {
            var approveFunction = new ApproveFunction();
                approveFunction.To = to;
                approveFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(approveFunction, cancellationToken);
        }

        public Task<BigInteger> BadgesClaimedQueryAsync(BadgesClaimedFunction badgesClaimedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BadgesClaimedFunction, BigInteger>(badgesClaimedFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> BadgesClaimedQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BadgesClaimedFunction, BigInteger>(null, blockParameter);
        }

        public Task<BigInteger> BalanceOfQueryAsync(BalanceOfFunction balanceOfFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOfFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> BalanceOfQueryAsync(string owner, BlockParameter blockParameter = null)
        {
            var balanceOfFunction = new BalanceOfFunction();
                balanceOfFunction.Owner = owner;
            
            return ContractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOfFunction, blockParameter);
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

        public Task<BigInteger> CheckpointCountQueryAsync(CheckpointCountFunction checkpointCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CheckpointCountFunction, BigInteger>(checkpointCountFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> CheckpointCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CheckpointCountFunction, BigInteger>(null, blockParameter);
        }

        public virtual Task<string> CheckpointRedemptionsRequestAsync(CheckpointRedemptionsFunction checkpointRedemptionsFunction)
        {
             return ContractHandler.SendRequestAsync(checkpointRedemptionsFunction);
        }

        public virtual Task<TransactionReceipt> CheckpointRedemptionsRequestAndWaitForReceiptAsync(CheckpointRedemptionsFunction checkpointRedemptionsFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(checkpointRedemptionsFunction, cancellationToken);
        }

        public virtual Task<string> CheckpointRedemptionsRequestAsync(byte[] root, BigInteger count)
        {
            var checkpointRedemptionsFunction = new CheckpointRedemptionsFunction();
                checkpointRedemptionsFunction.Root = root;
                checkpointRedemptionsFunction.Count = count;
            
             return ContractHandler.SendRequestAsync(checkpointRedemptionsFunction);
        }

        public virtual Task<TransactionReceipt> CheckpointRedemptionsRequestAndWaitForReceiptAsync(byte[] root, BigInteger count, CancellationTokenSource cancellationToken = null)
        {
            var checkpointRedemptionsFunction = new CheckpointRedemptionsFunction();
                checkpointRedemptionsFunction.Root = root;
                checkpointRedemptionsFunction.Count = count;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(checkpointRedemptionsFunction, cancellationToken);
        }

        public virtual Task<string> ClaimAttendanceBadgeRequestAsync(ClaimAttendanceBadgeFunction claimAttendanceBadgeFunction)
        {
             return ContractHandler.SendRequestAsync(claimAttendanceBadgeFunction);
        }

        public virtual Task<TransactionReceipt> ClaimAttendanceBadgeRequestAndWaitForReceiptAsync(ClaimAttendanceBadgeFunction claimAttendanceBadgeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(claimAttendanceBadgeFunction, cancellationToken);
        }

        public virtual Task<string> ClaimAttendanceBadgeRequestAsync(BigInteger tokenId, List<byte[]> proof)
        {
            var claimAttendanceBadgeFunction = new ClaimAttendanceBadgeFunction();
                claimAttendanceBadgeFunction.TokenId = tokenId;
                claimAttendanceBadgeFunction.Proof = proof;
            
             return ContractHandler.SendRequestAsync(claimAttendanceBadgeFunction);
        }

        public virtual Task<TransactionReceipt> ClaimAttendanceBadgeRequestAndWaitForReceiptAsync(BigInteger tokenId, List<byte[]> proof, CancellationTokenSource cancellationToken = null)
        {
            var claimAttendanceBadgeFunction = new ClaimAttendanceBadgeFunction();
                claimAttendanceBadgeFunction.TokenId = tokenId;
                claimAttendanceBadgeFunction.Proof = proof;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(claimAttendanceBadgeFunction, cancellationToken);
        }

        public virtual Task<string> ConfigureTierRequestAsync(ConfigureTierFunction configureTierFunction)
        {
             return ContractHandler.SendRequestAsync(configureTierFunction);
        }

        public virtual Task<TransactionReceipt> ConfigureTierRequestAndWaitForReceiptAsync(ConfigureTierFunction configureTierFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(configureTierFunction, cancellationToken);
        }

        public virtual Task<string> ConfigureTierRequestAsync(BigInteger tierId, string tierName, BigInteger price, BigInteger supplyCap, BigInteger maxPerWallet)
        {
            var configureTierFunction = new ConfigureTierFunction();
                configureTierFunction.TierId = tierId;
                configureTierFunction.TierName = tierName;
                configureTierFunction.Price = price;
                configureTierFunction.SupplyCap = supplyCap;
                configureTierFunction.MaxPerWallet = maxPerWallet;
            
             return ContractHandler.SendRequestAsync(configureTierFunction);
        }

        public virtual Task<TransactionReceipt> ConfigureTierRequestAndWaitForReceiptAsync(BigInteger tierId, string tierName, BigInteger price, BigInteger supplyCap, BigInteger maxPerWallet, CancellationTokenSource cancellationToken = null)
        {
            var configureTierFunction = new ConfigureTierFunction();
                configureTierFunction.TierId = tierId;
                configureTierFunction.TierName = tierName;
                configureTierFunction.Price = price;
                configureTierFunction.SupplyCap = supplyCap;
                configureTierFunction.MaxPerWallet = maxPerWallet;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(configureTierFunction, cancellationToken);
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

        public virtual Task<Eip712DomainOutputDTO> Eip712DomainQueryAsync(Eip712DomainFunction eip712DomainFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<Eip712DomainFunction, Eip712DomainOutputDTO>(eip712DomainFunction, blockParameter);
        }

        public virtual Task<Eip712DomainOutputDTO> Eip712DomainQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<Eip712DomainFunction, Eip712DomainOutputDTO>(null, blockParameter);
        }

        public Task<string> EscrowQueryAsync(EscrowFunction escrowFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EscrowFunction, string>(escrowFunction, blockParameter);
        }

        
        public virtual Task<string> EscrowQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EscrowFunction, string>(null, blockParameter);
        }

        public Task<ulong> EventEndQueryAsync(EventEndFunction eventEndFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventEndFunction, ulong>(eventEndFunction, blockParameter);
        }

        
        public virtual Task<ulong> EventEndQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventEndFunction, ulong>(null, blockParameter);
        }

        public Task<ulong> EventStartQueryAsync(EventStartFunction eventStartFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventStartFunction, ulong>(eventStartFunction, blockParameter);
        }

        
        public virtual Task<ulong> EventStartQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<EventStartFunction, ulong>(null, blockParameter);
        }

        public Task<string> GetApprovedQueryAsync(GetApprovedFunction getApprovedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetApprovedFunction, string>(getApprovedFunction, blockParameter);
        }

        
        public virtual Task<string> GetApprovedQueryAsync(BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var getApprovedFunction = new GetApprovedFunction();
                getApprovedFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<GetApprovedFunction, string>(getApprovedFunction, blockParameter);
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

        public Task<byte[]> HashVoucherQueryAsync(HashVoucherFunction hashVoucherFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<HashVoucherFunction, byte[]>(hashVoucherFunction, blockParameter);
        }

        
        public virtual Task<byte[]> HashVoucherQueryAsync(MintVoucher voucher, BlockParameter blockParameter = null)
        {
            var hashVoucherFunction = new HashVoucherFunction();
                hashVoucherFunction.Voucher = voucher;
            
            return ContractHandler.QueryAsync<HashVoucherFunction, byte[]>(hashVoucherFunction, blockParameter);
        }

        public virtual Task<string> InitializeRequestAsync(InitializeFunction initializeFunction)
        {
             return ContractHandler.SendRequestAsync(initializeFunction);
        }

        public virtual Task<TransactionReceipt> InitializeRequestAndWaitForReceiptAsync(InitializeFunction initializeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(initializeFunction, cancellationToken);
        }

        public virtual Task<string> InitializeRequestAsync(TicketInit p)
        {
            var initializeFunction = new InitializeFunction();
                initializeFunction.P = p;
            
             return ContractHandler.SendRequestAsync(initializeFunction);
        }

        public virtual Task<TransactionReceipt> InitializeRequestAndWaitForReceiptAsync(TicketInit p, CancellationTokenSource cancellationToken = null)
        {
            var initializeFunction = new InitializeFunction();
                initializeFunction.P = p;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(initializeFunction, cancellationToken);
        }

        public Task<bool> IsApprovedForAllQueryAsync(IsApprovedForAllFunction isApprovedForAllFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<IsApprovedForAllFunction, bool>(isApprovedForAllFunction, blockParameter);
        }

        
        public virtual Task<bool> IsApprovedForAllQueryAsync(string owner, string @operator, BlockParameter blockParameter = null)
        {
            var isApprovedForAllFunction = new IsApprovedForAllFunction();
                isApprovedForAllFunction.Owner = owner;
                isApprovedForAllFunction.Operator = @operator;
            
            return ContractHandler.QueryAsync<IsApprovedForAllFunction, bool>(isApprovedForAllFunction, blockParameter);
        }

        public Task<bool> IsRedeemedQueryAsync(IsRedeemedFunction isRedeemedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<IsRedeemedFunction, bool>(isRedeemedFunction, blockParameter);
        }

        
        public virtual Task<bool> IsRedeemedQueryAsync(BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var isRedeemedFunction = new IsRedeemedFunction();
                isRedeemedFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<IsRedeemedFunction, bool>(isRedeemedFunction, blockParameter);
        }

        public Task<bool> IsTransferableQueryAsync(IsTransferableFunction isTransferableFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<IsTransferableFunction, bool>(isTransferableFunction, blockParameter);
        }

        
        public virtual Task<bool> IsTransferableQueryAsync(BigInteger tokenId, string @operator, BlockParameter blockParameter = null)
        {
            var isTransferableFunction = new IsTransferableFunction();
                isTransferableFunction.TokenId = tokenId;
                isTransferableFunction.Operator = @operator;
            
            return ContractHandler.QueryAsync<IsTransferableFunction, bool>(isTransferableFunction, blockParameter);
        }

        public virtual Task<string> MintBatchRequestAsync(MintBatchFunction mintBatchFunction)
        {
             return ContractHandler.SendRequestAsync(mintBatchFunction);
        }

        public virtual Task<TransactionReceipt> MintBatchRequestAndWaitForReceiptAsync(MintBatchFunction mintBatchFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(mintBatchFunction, cancellationToken);
        }

        public virtual Task<string> MintBatchRequestAsync(List<MintOrder> orders)
        {
            var mintBatchFunction = new MintBatchFunction();
                mintBatchFunction.Orders = orders;
            
             return ContractHandler.SendRequestAsync(mintBatchFunction);
        }

        public virtual Task<TransactionReceipt> MintBatchRequestAndWaitForReceiptAsync(List<MintOrder> orders, CancellationTokenSource cancellationToken = null)
        {
            var mintBatchFunction = new MintBatchFunction();
                mintBatchFunction.Orders = orders;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(mintBatchFunction, cancellationToken);
        }

        public virtual Task<string> MintWithVoucherRequestAsync(MintWithVoucherFunction mintWithVoucherFunction)
        {
             return ContractHandler.SendRequestAsync(mintWithVoucherFunction);
        }

        public virtual Task<TransactionReceipt> MintWithVoucherRequestAndWaitForReceiptAsync(MintWithVoucherFunction mintWithVoucherFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(mintWithVoucherFunction, cancellationToken);
        }

        public virtual Task<string> MintWithVoucherRequestAsync(MintVoucher voucher, byte[] signature)
        {
            var mintWithVoucherFunction = new MintWithVoucherFunction();
                mintWithVoucherFunction.Voucher = voucher;
                mintWithVoucherFunction.Signature = signature;
            
             return ContractHandler.SendRequestAsync(mintWithVoucherFunction);
        }

        public virtual Task<TransactionReceipt> MintWithVoucherRequestAndWaitForReceiptAsync(MintVoucher voucher, byte[] signature, CancellationTokenSource cancellationToken = null)
        {
            var mintWithVoucherFunction = new MintWithVoucherFunction();
                mintWithVoucherFunction.Voucher = voucher;
                mintWithVoucherFunction.Signature = signature;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(mintWithVoucherFunction, cancellationToken);
        }

        public Task<BigInteger> MintedByQueryAsync(MintedByFunction mintedByFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MintedByFunction, BigInteger>(mintedByFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> MintedByQueryAsync(BigInteger tierId, string wallet, BlockParameter blockParameter = null)
        {
            var mintedByFunction = new MintedByFunction();
                mintedByFunction.TierId = tierId;
                mintedByFunction.Wallet = wallet;
            
            return ContractHandler.QueryAsync<MintedByFunction, BigInteger>(mintedByFunction, blockParameter);
        }

        public Task<string> NameQueryAsync(NameFunction nameFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NameFunction, string>(nameFunction, blockParameter);
        }

        
        public virtual Task<string> NameQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NameFunction, string>(null, blockParameter);
        }

        public Task<bool> NonceUsedQueryAsync(NonceUsedFunction nonceUsedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NonceUsedFunction, bool>(nonceUsedFunction, blockParameter);
        }

        
        public virtual Task<bool> NonceUsedQueryAsync(string buyer, BigInteger nonce, BlockParameter blockParameter = null)
        {
            var nonceUsedFunction = new NonceUsedFunction();
                nonceUsedFunction.Buyer = buyer;
                nonceUsedFunction.Nonce = nonce;
            
            return ContractHandler.QueryAsync<NonceUsedFunction, bool>(nonceUsedFunction, blockParameter);
        }

        public Task<string> OrganizerQueryAsync(OrganizerFunction organizerFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OrganizerFunction, string>(organizerFunction, blockParameter);
        }

        
        public virtual Task<string> OrganizerQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OrganizerFunction, string>(null, blockParameter);
        }

        public Task<string> OwnerQueryAsync(OwnerFunction ownerFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(ownerFunction, blockParameter);
        }

        
        public virtual Task<string> OwnerQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(null, blockParameter);
        }

        public Task<string> OwnerOfQueryAsync(OwnerOfFunction ownerOfFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerOfFunction, string>(ownerOfFunction, blockParameter);
        }

        
        public virtual Task<string> OwnerOfQueryAsync(BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var ownerOfFunction = new OwnerOfFunction();
                ownerOfFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<OwnerOfFunction, string>(ownerOfFunction, blockParameter);
        }

        public virtual Task<string> PauseRequestAsync(PauseFunction pauseFunction)
        {
             return ContractHandler.SendRequestAsync(pauseFunction);
        }

        public virtual Task<string> PauseRequestAsync()
        {
             return ContractHandler.SendRequestAsync<PauseFunction>();
        }

        public virtual Task<TransactionReceipt> PauseRequestAndWaitForReceiptAsync(PauseFunction pauseFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(pauseFunction, cancellationToken);
        }

        public virtual Task<TransactionReceipt> PauseRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<PauseFunction>(null, cancellationToken);
        }

        public Task<bool> PausedQueryAsync(PausedFunction pausedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PausedFunction, bool>(pausedFunction, blockParameter);
        }

        
        public virtual Task<bool> PausedQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PausedFunction, bool>(null, blockParameter);
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

        public Task<BigInteger> RedemptionCountQueryAsync(RedemptionCountFunction redemptionCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RedemptionCountFunction, BigInteger>(redemptionCountFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> RedemptionCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RedemptionCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<byte[]> RedemptionRootQueryAsync(RedemptionRootFunction redemptionRootFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RedemptionRootFunction, byte[]>(redemptionRootFunction, blockParameter);
        }

        
        public virtual Task<byte[]> RedemptionRootQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RedemptionRootFunction, byte[]>(null, blockParameter);
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

        public Task<string> ResaleContractQueryAsync(ResaleContractFunction resaleContractFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ResaleContractFunction, string>(resaleContractFunction, blockParameter);
        }

        
        public virtual Task<string> ResaleContractQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ResaleContractFunction, string>(null, blockParameter);
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

        public virtual Task<RoyaltyInfoOutputDTO> RoyaltyInfoQueryAsync(RoyaltyInfoFunction royaltyInfoFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<RoyaltyInfoFunction, RoyaltyInfoOutputDTO>(royaltyInfoFunction, blockParameter);
        }

        public virtual Task<RoyaltyInfoOutputDTO> RoyaltyInfoQueryAsync(BigInteger tokenId, BigInteger salePrice, BlockParameter blockParameter = null)
        {
            var royaltyInfoFunction = new RoyaltyInfoFunction();
                royaltyInfoFunction.TokenId = tokenId;
                royaltyInfoFunction.SalePrice = salePrice;
            
            return ContractHandler.QueryDeserializingToObjectAsync<RoyaltyInfoFunction, RoyaltyInfoOutputDTO>(royaltyInfoFunction, blockParameter);
        }

        public virtual Task<string> SafeTransferFromRequestAsync(SafeTransferFromFunction safeTransferFromFunction)
        {
             return ContractHandler.SendRequestAsync(safeTransferFromFunction);
        }

        public virtual Task<TransactionReceipt> SafeTransferFromRequestAndWaitForReceiptAsync(SafeTransferFromFunction safeTransferFromFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(safeTransferFromFunction, cancellationToken);
        }

        public virtual Task<string> SafeTransferFromRequestAsync(string from, string to, BigInteger tokenId)
        {
            var safeTransferFromFunction = new SafeTransferFromFunction();
                safeTransferFromFunction.From = from;
                safeTransferFromFunction.To = to;
                safeTransferFromFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAsync(safeTransferFromFunction);
        }

        public virtual Task<TransactionReceipt> SafeTransferFromRequestAndWaitForReceiptAsync(string from, string to, BigInteger tokenId, CancellationTokenSource cancellationToken = null)
        {
            var safeTransferFromFunction = new SafeTransferFromFunction();
                safeTransferFromFunction.From = from;
                safeTransferFromFunction.To = to;
                safeTransferFromFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(safeTransferFromFunction, cancellationToken);
        }

        public virtual Task<string> SafeTransferFromRequestAsync(SafeTransferFrom1Function safeTransferFrom1Function)
        {
             return ContractHandler.SendRequestAsync(safeTransferFrom1Function);
        }

        public virtual Task<TransactionReceipt> SafeTransferFromRequestAndWaitForReceiptAsync(SafeTransferFrom1Function safeTransferFrom1Function, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(safeTransferFrom1Function, cancellationToken);
        }

        public virtual Task<string> SafeTransferFromRequestAsync(string from, string to, BigInteger tokenId, byte[] data)
        {
            var safeTransferFrom1Function = new SafeTransferFrom1Function();
                safeTransferFrom1Function.From = from;
                safeTransferFrom1Function.To = to;
                safeTransferFrom1Function.TokenId = tokenId;
                safeTransferFrom1Function.Data = data;
            
             return ContractHandler.SendRequestAsync(safeTransferFrom1Function);
        }

        public virtual Task<TransactionReceipt> SafeTransferFromRequestAndWaitForReceiptAsync(string from, string to, BigInteger tokenId, byte[] data, CancellationTokenSource cancellationToken = null)
        {
            var safeTransferFrom1Function = new SafeTransferFrom1Function();
                safeTransferFrom1Function.From = from;
                safeTransferFrom1Function.To = to;
                safeTransferFrom1Function.TokenId = tokenId;
                safeTransferFrom1Function.Data = data;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(safeTransferFrom1Function, cancellationToken);
        }

        public virtual Task<string> SetApprovalForAllRequestAsync(SetApprovalForAllFunction setApprovalForAllFunction)
        {
             return ContractHandler.SendRequestAsync(setApprovalForAllFunction);
        }

        public virtual Task<TransactionReceipt> SetApprovalForAllRequestAndWaitForReceiptAsync(SetApprovalForAllFunction setApprovalForAllFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setApprovalForAllFunction, cancellationToken);
        }

        public virtual Task<string> SetApprovalForAllRequestAsync(string @operator, bool approved)
        {
            var setApprovalForAllFunction = new SetApprovalForAllFunction();
                setApprovalForAllFunction.Operator = @operator;
                setApprovalForAllFunction.Approved = approved;
            
             return ContractHandler.SendRequestAsync(setApprovalForAllFunction);
        }

        public virtual Task<TransactionReceipt> SetApprovalForAllRequestAndWaitForReceiptAsync(string @operator, bool approved, CancellationTokenSource cancellationToken = null)
        {
            var setApprovalForAllFunction = new SetApprovalForAllFunction();
                setApprovalForAllFunction.Operator = @operator;
                setApprovalForAllFunction.Approved = approved;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setApprovalForAllFunction, cancellationToken);
        }

        public virtual Task<string> SetResaleContractRequestAsync(SetResaleContractFunction setResaleContractFunction)
        {
             return ContractHandler.SendRequestAsync(setResaleContractFunction);
        }

        public virtual Task<TransactionReceipt> SetResaleContractRequestAndWaitForReceiptAsync(SetResaleContractFunction setResaleContractFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setResaleContractFunction, cancellationToken);
        }

        public virtual Task<string> SetResaleContractRequestAsync(string newResaleContract)
        {
            var setResaleContractFunction = new SetResaleContractFunction();
                setResaleContractFunction.NewResaleContract = newResaleContract;
            
             return ContractHandler.SendRequestAsync(setResaleContractFunction);
        }

        public virtual Task<TransactionReceipt> SetResaleContractRequestAndWaitForReceiptAsync(string newResaleContract, CancellationTokenSource cancellationToken = null)
        {
            var setResaleContractFunction = new SetResaleContractFunction();
                setResaleContractFunction.NewResaleContract = newResaleContract;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setResaleContractFunction, cancellationToken);
        }

        public virtual Task<string> SetUnlockTimeRequestAsync(SetUnlockTimeFunction setUnlockTimeFunction)
        {
             return ContractHandler.SendRequestAsync(setUnlockTimeFunction);
        }

        public virtual Task<TransactionReceipt> SetUnlockTimeRequestAndWaitForReceiptAsync(SetUnlockTimeFunction setUnlockTimeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setUnlockTimeFunction, cancellationToken);
        }

        public virtual Task<string> SetUnlockTimeRequestAsync(ulong newUnlockTime)
        {
            var setUnlockTimeFunction = new SetUnlockTimeFunction();
                setUnlockTimeFunction.NewUnlockTime = newUnlockTime;
            
             return ContractHandler.SendRequestAsync(setUnlockTimeFunction);
        }

        public virtual Task<TransactionReceipt> SetUnlockTimeRequestAndWaitForReceiptAsync(ulong newUnlockTime, CancellationTokenSource cancellationToken = null)
        {
            var setUnlockTimeFunction = new SetUnlockTimeFunction();
                setUnlockTimeFunction.NewUnlockTime = newUnlockTime;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setUnlockTimeFunction, cancellationToken);
        }

        public virtual Task<string> SetVoucherSignerRequestAsync(SetVoucherSignerFunction setVoucherSignerFunction)
        {
             return ContractHandler.SendRequestAsync(setVoucherSignerFunction);
        }

        public virtual Task<TransactionReceipt> SetVoucherSignerRequestAndWaitForReceiptAsync(SetVoucherSignerFunction setVoucherSignerFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setVoucherSignerFunction, cancellationToken);
        }

        public virtual Task<string> SetVoucherSignerRequestAsync(string signer)
        {
            var setVoucherSignerFunction = new SetVoucherSignerFunction();
                setVoucherSignerFunction.Signer = signer;
            
             return ContractHandler.SendRequestAsync(setVoucherSignerFunction);
        }

        public virtual Task<TransactionReceipt> SetVoucherSignerRequestAndWaitForReceiptAsync(string signer, CancellationTokenSource cancellationToken = null)
        {
            var setVoucherSignerFunction = new SetVoucherSignerFunction();
                setVoucherSignerFunction.Signer = signer;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setVoucherSignerFunction, cancellationToken);
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

        public Task<string> SymbolQueryAsync(SymbolFunction symbolFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<SymbolFunction, string>(symbolFunction, blockParameter);
        }

        
        public virtual Task<string> SymbolQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<SymbolFunction, string>(null, blockParameter);
        }

        public virtual Task<TierConfigOutputDTO> TierConfigQueryAsync(TierConfigFunction tierConfigFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<TierConfigFunction, TierConfigOutputDTO>(tierConfigFunction, blockParameter);
        }

        public virtual Task<TierConfigOutputDTO> TierConfigQueryAsync(BigInteger tierId, BlockParameter blockParameter = null)
        {
            var tierConfigFunction = new TierConfigFunction();
                tierConfigFunction.TierId = tierId;
            
            return ContractHandler.QueryDeserializingToObjectAsync<TierConfigFunction, TierConfigOutputDTO>(tierConfigFunction, blockParameter);
        }

        public Task<BigInteger> TierOfQueryAsync(TierOfFunction tierOfFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TierOfFunction, BigInteger>(tierOfFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> TierOfQueryAsync(BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var tierOfFunction = new TierOfFunction();
                tierOfFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<TierOfFunction, BigInteger>(tierOfFunction, blockParameter);
        }

        public Task<BigInteger> TierPriceQueryAsync(TierPriceFunction tierPriceFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TierPriceFunction, BigInteger>(tierPriceFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> TierPriceQueryAsync(BigInteger tierId, BlockParameter blockParameter = null)
        {
            var tierPriceFunction = new TierPriceFunction();
                tierPriceFunction.TierId = tierId;
            
            return ContractHandler.QueryAsync<TierPriceFunction, BigInteger>(tierPriceFunction, blockParameter);
        }

        public Task<string> TokenURIQueryAsync(TokenURIFunction tokenURIFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TokenURIFunction, string>(tokenURIFunction, blockParameter);
        }

        
        public virtual Task<string> TokenURIQueryAsync(BigInteger tokenId, BlockParameter blockParameter = null)
        {
            var tokenURIFunction = new TokenURIFunction();
                tokenURIFunction.TokenId = tokenId;
            
            return ContractHandler.QueryAsync<TokenURIFunction, string>(tokenURIFunction, blockParameter);
        }

        public Task<BigInteger> TotalMintedQueryAsync(TotalMintedFunction totalMintedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalMintedFunction, BigInteger>(totalMintedFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> TotalMintedQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalMintedFunction, BigInteger>(null, blockParameter);
        }

        public virtual Task<string> TransferFromRequestAsync(TransferFromFunction transferFromFunction)
        {
             return ContractHandler.SendRequestAsync(transferFromFunction);
        }

        public virtual Task<TransactionReceipt> TransferFromRequestAndWaitForReceiptAsync(TransferFromFunction transferFromFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferFromFunction, cancellationToken);
        }

        public virtual Task<string> TransferFromRequestAsync(string from, string to, BigInteger tokenId)
        {
            var transferFromFunction = new TransferFromFunction();
                transferFromFunction.From = from;
                transferFromFunction.To = to;
                transferFromFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAsync(transferFromFunction);
        }

        public virtual Task<TransactionReceipt> TransferFromRequestAndWaitForReceiptAsync(string from, string to, BigInteger tokenId, CancellationTokenSource cancellationToken = null)
        {
            var transferFromFunction = new TransferFromFunction();
                transferFromFunction.From = from;
                transferFromFunction.To = to;
                transferFromFunction.TokenId = tokenId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferFromFunction, cancellationToken);
        }

        public Task<ulong> UnlockTimeQueryAsync(UnlockTimeFunction unlockTimeFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UnlockTimeFunction, ulong>(unlockTimeFunction, blockParameter);
        }

        
        public virtual Task<ulong> UnlockTimeQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UnlockTimeFunction, ulong>(null, blockParameter);
        }

        public virtual Task<string> UnpauseRequestAsync(UnpauseFunction unpauseFunction)
        {
             return ContractHandler.SendRequestAsync(unpauseFunction);
        }

        public virtual Task<string> UnpauseRequestAsync()
        {
             return ContractHandler.SendRequestAsync<UnpauseFunction>();
        }

        public virtual Task<TransactionReceipt> UnpauseRequestAndWaitForReceiptAsync(UnpauseFunction unpauseFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(unpauseFunction, cancellationToken);
        }

        public virtual Task<TransactionReceipt> UnpauseRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<UnpauseFunction>(null, cancellationToken);
        }

        public Task<string> VoucherSignerQueryAsync(VoucherSignerFunction voucherSignerFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<VoucherSignerFunction, string>(voucherSignerFunction, blockParameter);
        }

        
        public virtual Task<string> VoucherSignerQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<VoucherSignerFunction, string>(null, blockParameter);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(DefaultAdminRoleFunction),
                typeof(MinterRoleFunction),
                typeof(MintVoucherTypehashFunction),
                typeof(PauserRoleFunction),
                typeof(RedeemerRoleFunction),
                typeof(AcceptDefaultAdminTransferFunction),
                typeof(ApproveFunction),
                typeof(BadgesClaimedFunction),
                typeof(BalanceOfFunction),
                typeof(BeginDefaultAdminTransferFunction),
                typeof(CancelDefaultAdminTransferFunction),
                typeof(ChangeDefaultAdminDelayFunction),
                typeof(CheckpointCountFunction),
                typeof(CheckpointRedemptionsFunction),
                typeof(ClaimAttendanceBadgeFunction),
                typeof(ConfigureTierFunction),
                typeof(DefaultAdminFunction),
                typeof(DefaultAdminDelayFunction),
                typeof(DefaultAdminDelayIncreaseWaitFunction),
                typeof(Eip712DomainFunction),
                typeof(EscrowFunction),
                typeof(EventEndFunction),
                typeof(EventStartFunction),
                typeof(GetApprovedFunction),
                typeof(GetRoleAdminFunction),
                typeof(GrantRoleFunction),
                typeof(HasRoleFunction),
                typeof(HashVoucherFunction),
                typeof(InitializeFunction),
                typeof(IsApprovedForAllFunction),
                typeof(IsRedeemedFunction),
                typeof(IsTransferableFunction),
                typeof(MintBatchFunction),
                typeof(MintWithVoucherFunction),
                typeof(MintedByFunction),
                typeof(NameFunction),
                typeof(NonceUsedFunction),
                typeof(OrganizerFunction),
                typeof(OwnerFunction),
                typeof(OwnerOfFunction),
                typeof(PauseFunction),
                typeof(PausedFunction),
                typeof(PendingDefaultAdminFunction),
                typeof(PendingDefaultAdminDelayFunction),
                typeof(RedemptionCountFunction),
                typeof(RedemptionRootFunction),
                typeof(RendererFunction),
                typeof(RenounceRoleFunction),
                typeof(ResaleContractFunction),
                typeof(RevokeRoleFunction),
                typeof(RollbackDefaultAdminDelayFunction),
                typeof(RoyaltyInfoFunction),
                typeof(SafeTransferFromFunction),
                typeof(SafeTransferFrom1Function),
                typeof(SetApprovalForAllFunction),
                typeof(SetResaleContractFunction),
                typeof(SetUnlockTimeFunction),
                typeof(SetVoucherSignerFunction),
                typeof(SupportsInterfaceFunction),
                typeof(SymbolFunction),
                typeof(TierConfigFunction),
                typeof(TierOfFunction),
                typeof(TierPriceFunction),
                typeof(TokenURIFunction),
                typeof(TotalMintedFunction),
                typeof(TransferFromFunction),
                typeof(UnlockTimeFunction),
                typeof(UnpauseFunction),
                typeof(VoucherSignerFunction)
            };
        }

        public override List<Type> GetAllEventTypes()
        {
            return new List<Type>
            {
                typeof(ApprovalEventDTO),
                typeof(ApprovalForAllEventDTO),
                typeof(DefaultAdminDelayChangeCanceledEventDTO),
                typeof(DefaultAdminDelayChangeScheduledEventDTO),
                typeof(DefaultAdminTransferCanceledEventDTO),
                typeof(DefaultAdminTransferScheduledEventDTO),
                typeof(EIP712DomainChangedEventDTO),
                typeof(InitializedEventDTO),
                typeof(MintedEventDTO),
                typeof(OrderRejectedEventDTO),
                typeof(PausedEventDTO),
                typeof(RedeemedEventDTO),
                typeof(RedemptionsCheckpointedEventDTO),
                typeof(ResaleContractSetEventDTO),
                typeof(RoleAdminChangedEventDTO),
                typeof(RoleGrantedEventDTO),
                typeof(RoleRevokedEventDTO),
                typeof(TierConfiguredEventDTO),
                typeof(TransferEventDTO),
                typeof(UnlockTimeSetEventDTO),
                typeof(UnpausedEventDTO),
                typeof(VoucherSignerSetEventDTO)
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
                typeof(AlreadyRedeemedError),
                typeof(ERC2981InvalidDefaultRoyaltyError),
                typeof(ERC2981InvalidDefaultRoyaltyReceiverError),
                typeof(ERC2981InvalidTokenRoyaltyError),
                typeof(ERC2981InvalidTokenRoyaltyReceiverError),
                typeof(ERC721IncorrectOwnerError),
                typeof(ERC721InsufficientApprovalError),
                typeof(ERC721InvalidApproverError),
                typeof(ERC721InvalidOperatorError),
                typeof(ERC721InvalidOwnerError),
                typeof(ERC721InvalidReceiverError),
                typeof(ERC721InvalidSenderError),
                typeof(ERC721NonexistentTokenError),
                typeof(EnforcedPauseError),
                typeof(ExpectedPauseError),
                typeof(InvalidCheckpointError),
                typeof(InvalidInitializationError),
                typeof(InvalidProofError),
                typeof(InvalidScheduleError),
                typeof(InvalidTierConfigError),
                typeof(InvalidVoucherSignatureError),
                typeof(MaxPerWalletExceededError),
                typeof(NotInitializingError),
                typeof(SafeCastOverflowedUintDowncastError),
                typeof(SoulboundError),
                typeof(SupplyCapExceededError),
                typeof(TicketListedError),
                typeof(TransferLockedError),
                typeof(UnknownTierError),
                typeof(VoucherAlreadyUsedError),
                typeof(VoucherExpiredError),
                typeof(ZeroAddressError),
                typeof(ZeroQuantityError)
            };
        }
    }
}
