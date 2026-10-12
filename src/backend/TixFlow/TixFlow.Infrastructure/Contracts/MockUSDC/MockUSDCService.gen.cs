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
using TixFlow.Infrastructure.Contracts.MockUSDC.ContractDefinition;

namespace TixFlow.Infrastructure.Contracts.MockUSDC
{
    public partial class MockUSDCService: MockUSDCServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, MockUSDCDeployment mockUSDCDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<MockUSDCDeployment>().SendRequestAndWaitForReceiptAsync(mockUSDCDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, MockUSDCDeployment mockUSDCDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<MockUSDCDeployment>().SendRequestAsync(mockUSDCDeployment);
        }

        public static async Task<MockUSDCService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, MockUSDCDeployment mockUSDCDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, mockUSDCDeployment, cancellationTokenSource);
            return new MockUSDCService(web3, receipt.ContractAddress);
        }

        public MockUSDCService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

    }


    public partial class MockUSDCServiceBase: ContractWeb3ServiceBase
    {

        public MockUSDCServiceBase(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

        public Task<byte[]> DomainSeparatorQueryAsync(DomainSeparatorFunction domainSeparatorFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DomainSeparatorFunction, byte[]>(domainSeparatorFunction, blockParameter);
        }

        
        public virtual Task<byte[]> DomainSeparatorQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DomainSeparatorFunction, byte[]>(null, blockParameter);
        }

        public Task<BigInteger> FaucetLimitQueryAsync(FaucetLimitFunction faucetLimitFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<FaucetLimitFunction, BigInteger>(faucetLimitFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> FaucetLimitQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<FaucetLimitFunction, BigInteger>(null, blockParameter);
        }

        public Task<byte[]> PermitTypehashQueryAsync(PermitTypehashFunction permitTypehashFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PermitTypehashFunction, byte[]>(permitTypehashFunction, blockParameter);
        }

        
        public virtual Task<byte[]> PermitTypehashQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PermitTypehashFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> ReceiveWithAuthorizationTypehashQueryAsync(ReceiveWithAuthorizationTypehashFunction receiveWithAuthorizationTypehashFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ReceiveWithAuthorizationTypehashFunction, byte[]>(receiveWithAuthorizationTypehashFunction, blockParameter);
        }

        
        public virtual Task<byte[]> ReceiveWithAuthorizationTypehashQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ReceiveWithAuthorizationTypehashFunction, byte[]>(null, blockParameter);
        }

        public Task<byte[]> TransferWithAuthorizationTypehashQueryAsync(TransferWithAuthorizationTypehashFunction transferWithAuthorizationTypehashFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TransferWithAuthorizationTypehashFunction, byte[]>(transferWithAuthorizationTypehashFunction, blockParameter);
        }

        
        public virtual Task<byte[]> TransferWithAuthorizationTypehashQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TransferWithAuthorizationTypehashFunction, byte[]>(null, blockParameter);
        }

        public Task<BigInteger> AllowanceQueryAsync(AllowanceFunction allowanceFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<AllowanceFunction, BigInteger>(allowanceFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> AllowanceQueryAsync(string owner, string spender, BlockParameter blockParameter = null)
        {
            var allowanceFunction = new AllowanceFunction();
                allowanceFunction.Owner = owner;
                allowanceFunction.Spender = spender;
            
            return ContractHandler.QueryAsync<AllowanceFunction, BigInteger>(allowanceFunction, blockParameter);
        }

        public virtual Task<string> ApproveRequestAsync(ApproveFunction approveFunction)
        {
             return ContractHandler.SendRequestAsync(approveFunction);
        }

        public virtual Task<TransactionReceipt> ApproveRequestAndWaitForReceiptAsync(ApproveFunction approveFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(approveFunction, cancellationToken);
        }

        public virtual Task<string> ApproveRequestAsync(string spender, BigInteger value)
        {
            var approveFunction = new ApproveFunction();
                approveFunction.Spender = spender;
                approveFunction.Value = value;
            
             return ContractHandler.SendRequestAsync(approveFunction);
        }

        public virtual Task<TransactionReceipt> ApproveRequestAndWaitForReceiptAsync(string spender, BigInteger value, CancellationTokenSource cancellationToken = null)
        {
            var approveFunction = new ApproveFunction();
                approveFunction.Spender = spender;
                approveFunction.Value = value;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(approveFunction, cancellationToken);
        }

        public Task<bool> AuthorizationStateQueryAsync(AuthorizationStateFunction authorizationStateFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<AuthorizationStateFunction, bool>(authorizationStateFunction, blockParameter);
        }

        
        public virtual Task<bool> AuthorizationStateQueryAsync(string authorizer, byte[] nonce, BlockParameter blockParameter = null)
        {
            var authorizationStateFunction = new AuthorizationStateFunction();
                authorizationStateFunction.Authorizer = authorizer;
                authorizationStateFunction.Nonce = nonce;
            
            return ContractHandler.QueryAsync<AuthorizationStateFunction, bool>(authorizationStateFunction, blockParameter);
        }

        public Task<BigInteger> BalanceOfQueryAsync(BalanceOfFunction balanceOfFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOfFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> BalanceOfQueryAsync(string account, BlockParameter blockParameter = null)
        {
            var balanceOfFunction = new BalanceOfFunction();
                balanceOfFunction.Account = account;
            
            return ContractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOfFunction, blockParameter);
        }

        public Task<byte> DecimalsQueryAsync(DecimalsFunction decimalsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DecimalsFunction, byte>(decimalsFunction, blockParameter);
        }

        
        public virtual Task<byte> DecimalsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DecimalsFunction, byte>(null, blockParameter);
        }

        public virtual Task<Eip712DomainOutputDTO> Eip712DomainQueryAsync(Eip712DomainFunction eip712DomainFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<Eip712DomainFunction, Eip712DomainOutputDTO>(eip712DomainFunction, blockParameter);
        }

        public virtual Task<Eip712DomainOutputDTO> Eip712DomainQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<Eip712DomainFunction, Eip712DomainOutputDTO>(null, blockParameter);
        }

        public virtual Task<string> MintRequestAsync(MintFunction mintFunction)
        {
             return ContractHandler.SendRequestAsync(mintFunction);
        }

        public virtual Task<TransactionReceipt> MintRequestAndWaitForReceiptAsync(MintFunction mintFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(mintFunction, cancellationToken);
        }

        public virtual Task<string> MintRequestAsync(string to, BigInteger amount)
        {
            var mintFunction = new MintFunction();
                mintFunction.To = to;
                mintFunction.Amount = amount;
            
             return ContractHandler.SendRequestAsync(mintFunction);
        }

        public virtual Task<TransactionReceipt> MintRequestAndWaitForReceiptAsync(string to, BigInteger amount, CancellationTokenSource cancellationToken = null)
        {
            var mintFunction = new MintFunction();
                mintFunction.To = to;
                mintFunction.Amount = amount;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(mintFunction, cancellationToken);
        }

        public Task<string> NameQueryAsync(NameFunction nameFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NameFunction, string>(nameFunction, blockParameter);
        }

        
        public virtual Task<string> NameQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NameFunction, string>(null, blockParameter);
        }

        public Task<BigInteger> NoncesQueryAsync(NoncesFunction noncesFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<NoncesFunction, BigInteger>(noncesFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> NoncesQueryAsync(string owner, BlockParameter blockParameter = null)
        {
            var noncesFunction = new NoncesFunction();
                noncesFunction.Owner = owner;
            
            return ContractHandler.QueryAsync<NoncesFunction, BigInteger>(noncesFunction, blockParameter);
        }

        public virtual Task<string> PermitRequestAsync(PermitFunction permitFunction)
        {
             return ContractHandler.SendRequestAsync(permitFunction);
        }

        public virtual Task<TransactionReceipt> PermitRequestAndWaitForReceiptAsync(PermitFunction permitFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(permitFunction, cancellationToken);
        }

        public virtual Task<string> PermitRequestAsync(string owner, string spender, BigInteger value, BigInteger deadline, byte v, byte[] r, byte[] s)
        {
            var permitFunction = new PermitFunction();
                permitFunction.Owner = owner;
                permitFunction.Spender = spender;
                permitFunction.Value = value;
                permitFunction.Deadline = deadline;
                permitFunction.V = v;
                permitFunction.R = r;
                permitFunction.S = s;
            
             return ContractHandler.SendRequestAsync(permitFunction);
        }

        public virtual Task<TransactionReceipt> PermitRequestAndWaitForReceiptAsync(string owner, string spender, BigInteger value, BigInteger deadline, byte v, byte[] r, byte[] s, CancellationTokenSource cancellationToken = null)
        {
            var permitFunction = new PermitFunction();
                permitFunction.Owner = owner;
                permitFunction.Spender = spender;
                permitFunction.Value = value;
                permitFunction.Deadline = deadline;
                permitFunction.V = v;
                permitFunction.R = r;
                permitFunction.S = s;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(permitFunction, cancellationToken);
        }

        public virtual Task<string> ReceiveWithAuthorizationRequestAsync(ReceiveWithAuthorizationFunction receiveWithAuthorizationFunction)
        {
             return ContractHandler.SendRequestAsync(receiveWithAuthorizationFunction);
        }

        public virtual Task<TransactionReceipt> ReceiveWithAuthorizationRequestAndWaitForReceiptAsync(ReceiveWithAuthorizationFunction receiveWithAuthorizationFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(receiveWithAuthorizationFunction, cancellationToken);
        }

        public virtual Task<string> ReceiveWithAuthorizationRequestAsync(string from, string to, BigInteger value, BigInteger validAfter, BigInteger validBefore, byte[] nonce, byte v, byte[] r, byte[] s)
        {
            var receiveWithAuthorizationFunction = new ReceiveWithAuthorizationFunction();
                receiveWithAuthorizationFunction.From = from;
                receiveWithAuthorizationFunction.To = to;
                receiveWithAuthorizationFunction.Value = value;
                receiveWithAuthorizationFunction.ValidAfter = validAfter;
                receiveWithAuthorizationFunction.ValidBefore = validBefore;
                receiveWithAuthorizationFunction.Nonce = nonce;
                receiveWithAuthorizationFunction.V = v;
                receiveWithAuthorizationFunction.R = r;
                receiveWithAuthorizationFunction.S = s;
            
             return ContractHandler.SendRequestAsync(receiveWithAuthorizationFunction);
        }

        public virtual Task<TransactionReceipt> ReceiveWithAuthorizationRequestAndWaitForReceiptAsync(string from, string to, BigInteger value, BigInteger validAfter, BigInteger validBefore, byte[] nonce, byte v, byte[] r, byte[] s, CancellationTokenSource cancellationToken = null)
        {
            var receiveWithAuthorizationFunction = new ReceiveWithAuthorizationFunction();
                receiveWithAuthorizationFunction.From = from;
                receiveWithAuthorizationFunction.To = to;
                receiveWithAuthorizationFunction.Value = value;
                receiveWithAuthorizationFunction.ValidAfter = validAfter;
                receiveWithAuthorizationFunction.ValidBefore = validBefore;
                receiveWithAuthorizationFunction.Nonce = nonce;
                receiveWithAuthorizationFunction.V = v;
                receiveWithAuthorizationFunction.R = r;
                receiveWithAuthorizationFunction.S = s;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(receiveWithAuthorizationFunction, cancellationToken);
        }

        public Task<string> SymbolQueryAsync(SymbolFunction symbolFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<SymbolFunction, string>(symbolFunction, blockParameter);
        }

        
        public virtual Task<string> SymbolQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<SymbolFunction, string>(null, blockParameter);
        }

        public Task<BigInteger> TotalSupplyQueryAsync(TotalSupplyFunction totalSupplyFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalSupplyFunction, BigInteger>(totalSupplyFunction, blockParameter);
        }

        
        public virtual Task<BigInteger> TotalSupplyQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalSupplyFunction, BigInteger>(null, blockParameter);
        }

        public virtual Task<string> TransferRequestAsync(TransferFunction transferFunction)
        {
             return ContractHandler.SendRequestAsync(transferFunction);
        }

        public virtual Task<TransactionReceipt> TransferRequestAndWaitForReceiptAsync(TransferFunction transferFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferFunction, cancellationToken);
        }

        public virtual Task<string> TransferRequestAsync(string to, BigInteger value)
        {
            var transferFunction = new TransferFunction();
                transferFunction.To = to;
                transferFunction.Value = value;
            
             return ContractHandler.SendRequestAsync(transferFunction);
        }

        public virtual Task<TransactionReceipt> TransferRequestAndWaitForReceiptAsync(string to, BigInteger value, CancellationTokenSource cancellationToken = null)
        {
            var transferFunction = new TransferFunction();
                transferFunction.To = to;
                transferFunction.Value = value;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferFunction, cancellationToken);
        }

        public virtual Task<string> TransferFromRequestAsync(TransferFromFunction transferFromFunction)
        {
             return ContractHandler.SendRequestAsync(transferFromFunction);
        }

        public virtual Task<TransactionReceipt> TransferFromRequestAndWaitForReceiptAsync(TransferFromFunction transferFromFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferFromFunction, cancellationToken);
        }

        public virtual Task<string> TransferFromRequestAsync(string from, string to, BigInteger value)
        {
            var transferFromFunction = new TransferFromFunction();
                transferFromFunction.From = from;
                transferFromFunction.To = to;
                transferFromFunction.Value = value;
            
             return ContractHandler.SendRequestAsync(transferFromFunction);
        }

        public virtual Task<TransactionReceipt> TransferFromRequestAndWaitForReceiptAsync(string from, string to, BigInteger value, CancellationTokenSource cancellationToken = null)
        {
            var transferFromFunction = new TransferFromFunction();
                transferFromFunction.From = from;
                transferFromFunction.To = to;
                transferFromFunction.Value = value;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferFromFunction, cancellationToken);
        }

        public virtual Task<string> TransferWithAuthorizationRequestAsync(TransferWithAuthorizationFunction transferWithAuthorizationFunction)
        {
             return ContractHandler.SendRequestAsync(transferWithAuthorizationFunction);
        }

        public virtual Task<TransactionReceipt> TransferWithAuthorizationRequestAndWaitForReceiptAsync(TransferWithAuthorizationFunction transferWithAuthorizationFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferWithAuthorizationFunction, cancellationToken);
        }

        public virtual Task<string> TransferWithAuthorizationRequestAsync(string from, string to, BigInteger value, BigInteger validAfter, BigInteger validBefore, byte[] nonce, byte v, byte[] r, byte[] s)
        {
            var transferWithAuthorizationFunction = new TransferWithAuthorizationFunction();
                transferWithAuthorizationFunction.From = from;
                transferWithAuthorizationFunction.To = to;
                transferWithAuthorizationFunction.Value = value;
                transferWithAuthorizationFunction.ValidAfter = validAfter;
                transferWithAuthorizationFunction.ValidBefore = validBefore;
                transferWithAuthorizationFunction.Nonce = nonce;
                transferWithAuthorizationFunction.V = v;
                transferWithAuthorizationFunction.R = r;
                transferWithAuthorizationFunction.S = s;
            
             return ContractHandler.SendRequestAsync(transferWithAuthorizationFunction);
        }

        public virtual Task<TransactionReceipt> TransferWithAuthorizationRequestAndWaitForReceiptAsync(string from, string to, BigInteger value, BigInteger validAfter, BigInteger validBefore, byte[] nonce, byte v, byte[] r, byte[] s, CancellationTokenSource cancellationToken = null)
        {
            var transferWithAuthorizationFunction = new TransferWithAuthorizationFunction();
                transferWithAuthorizationFunction.From = from;
                transferWithAuthorizationFunction.To = to;
                transferWithAuthorizationFunction.Value = value;
                transferWithAuthorizationFunction.ValidAfter = validAfter;
                transferWithAuthorizationFunction.ValidBefore = validBefore;
                transferWithAuthorizationFunction.Nonce = nonce;
                transferWithAuthorizationFunction.V = v;
                transferWithAuthorizationFunction.R = r;
                transferWithAuthorizationFunction.S = s;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferWithAuthorizationFunction, cancellationToken);
        }

        public Task<string> VersionQueryAsync(VersionFunction versionFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<VersionFunction, string>(versionFunction, blockParameter);
        }

        
        public virtual Task<string> VersionQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<VersionFunction, string>(null, blockParameter);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(DomainSeparatorFunction),
                typeof(FaucetLimitFunction),
                typeof(PermitTypehashFunction),
                typeof(ReceiveWithAuthorizationTypehashFunction),
                typeof(TransferWithAuthorizationTypehashFunction),
                typeof(AllowanceFunction),
                typeof(ApproveFunction),
                typeof(AuthorizationStateFunction),
                typeof(BalanceOfFunction),
                typeof(DecimalsFunction),
                typeof(Eip712DomainFunction),
                typeof(MintFunction),
                typeof(NameFunction),
                typeof(NoncesFunction),
                typeof(PermitFunction),
                typeof(ReceiveWithAuthorizationFunction),
                typeof(SymbolFunction),
                typeof(TotalSupplyFunction),
                typeof(TransferFunction),
                typeof(TransferFromFunction),
                typeof(TransferWithAuthorizationFunction),
                typeof(VersionFunction)
            };
        }

        public override List<Type> GetAllEventTypes()
        {
            return new List<Type>
            {
                typeof(ApprovalEventDTO),
                typeof(AuthorizationUsedEventDTO),
                typeof(EIP712DomainChangedEventDTO),
                typeof(TransferEventDTO)
            };
        }

        public override List<Type> GetAllErrorTypes()
        {
            return new List<Type>
            {
                typeof(AuthorizationAlreadyUsedError),
                typeof(AuthorizationExpiredError),
                typeof(AuthorizationNotYetValidError),
                typeof(CallerMustBePayeeError),
                typeof(ECDSAInvalidSignatureError),
                typeof(ECDSAInvalidSignatureLengthError),
                typeof(ECDSAInvalidSignatureSError),
                typeof(ERC20InsufficientAllowanceError),
                typeof(ERC20InsufficientBalanceError),
                typeof(ERC20InvalidApproverError),
                typeof(ERC20InvalidReceiverError),
                typeof(ERC20InvalidSenderError),
                typeof(ERC20InvalidSpenderError),
                typeof(FaucetLimitExceededError),
                typeof(InvalidShortStringError),
                typeof(InvalidSignatureError),
                typeof(PermitExpiredError),
                typeof(StringTooLongError)
            };
        }
    }
}
