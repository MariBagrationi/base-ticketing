using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition
{
    public partial class PermitSignature : PermitSignatureBase { }

    public class PermitSignatureBase 
    {
        [Parameter("uint256", "deadline", 1)]
        public virtual BigInteger Deadline { get; set; }
        [Parameter("uint8", "v", 2)]
        public virtual byte V { get; set; }
        [Parameter("bytes32", "r", 3)]
        public virtual byte[] R { get; set; }
        [Parameter("bytes32", "s", 4)]
        public virtual byte[] S { get; set; }
    }
}
