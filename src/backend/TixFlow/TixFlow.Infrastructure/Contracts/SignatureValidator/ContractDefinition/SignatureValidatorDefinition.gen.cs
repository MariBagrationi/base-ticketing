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

namespace TixFlow.Infrastructure.Contracts.SignatureValidator.ContractDefinition
{


    public partial class SignatureValidatorDeployment : SignatureValidatorDeploymentBase
    {
        public SignatureValidatorDeployment() : base(BYTECODE) { }
        public SignatureValidatorDeployment(string byteCode) : base(byteCode) { }
    }

    public class SignatureValidatorDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x";
        public SignatureValidatorDeploymentBase() : base(BYTECODE) { }
        public SignatureValidatorDeploymentBase(string byteCode) : base(byteCode) { }

    }

    public partial class IsValidSigFunction : IsValidSigFunctionBase { }

    [Function("isValidSig", "bool")]
    public class IsValidSigFunctionBase : FunctionMessage
    {
        [Parameter("address", "signer", 1)]
        public virtual string Signer { get; set; }
        [Parameter("bytes32", "hash", 2)]
        public virtual byte[] Hash { get; set; }
        [Parameter("bytes", "signature", 3)]
        public virtual byte[] Signature { get; set; }
    }

    public partial class WalletDeploymentFailedError : WalletDeploymentFailedErrorBase { }

    [Error("WalletDeploymentFailed")]
    public class WalletDeploymentFailedErrorBase : IErrorDTO
    {
        [Parameter("bytes", "reason", 1)]
        public virtual byte[] Reason { get; set; }
    }


}
