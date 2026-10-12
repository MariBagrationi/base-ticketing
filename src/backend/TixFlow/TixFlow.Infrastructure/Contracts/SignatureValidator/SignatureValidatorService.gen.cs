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
using TixFlow.Infrastructure.Contracts.SignatureValidator.ContractDefinition;

namespace TixFlow.Infrastructure.Contracts.SignatureValidator
{
    public partial class SignatureValidatorService: SignatureValidatorServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, SignatureValidatorDeployment signatureValidatorDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<SignatureValidatorDeployment>().SendRequestAndWaitForReceiptAsync(signatureValidatorDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, SignatureValidatorDeployment signatureValidatorDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<SignatureValidatorDeployment>().SendRequestAsync(signatureValidatorDeployment);
        }

        public static async Task<SignatureValidatorService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, SignatureValidatorDeployment signatureValidatorDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, signatureValidatorDeployment, cancellationTokenSource);
            return new SignatureValidatorService(web3, receipt.ContractAddress);
        }

        public SignatureValidatorService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

    }


    public partial class SignatureValidatorServiceBase: ContractWeb3ServiceBase
    {

        public SignatureValidatorServiceBase(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

        public virtual Task<string> IsValidSigRequestAsync(IsValidSigFunction isValidSigFunction)
        {
             return ContractHandler.SendRequestAsync(isValidSigFunction);
        }

        public virtual Task<TransactionReceipt> IsValidSigRequestAndWaitForReceiptAsync(IsValidSigFunction isValidSigFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(isValidSigFunction, cancellationToken);
        }

        public virtual Task<string> IsValidSigRequestAsync(string signer, byte[] hash, byte[] signature)
        {
            var isValidSigFunction = new IsValidSigFunction();
                isValidSigFunction.Signer = signer;
                isValidSigFunction.Hash = hash;
                isValidSigFunction.Signature = signature;
            
             return ContractHandler.SendRequestAsync(isValidSigFunction);
        }

        public virtual Task<TransactionReceipt> IsValidSigRequestAndWaitForReceiptAsync(string signer, byte[] hash, byte[] signature, CancellationTokenSource cancellationToken = null)
        {
            var isValidSigFunction = new IsValidSigFunction();
                isValidSigFunction.Signer = signer;
                isValidSigFunction.Hash = hash;
                isValidSigFunction.Signature = signature;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(isValidSigFunction, cancellationToken);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(IsValidSigFunction)
            };
        }

        public override List<Type> GetAllEventTypes()
        {
            return new List<Type>
            {

            };
        }

        public override List<Type> GetAllErrorTypes()
        {
            return new List<Type>
            {
                typeof(WalletDeploymentFailedError)
            };
        }
    }
}
