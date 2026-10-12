using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition
{
    public partial class PaymentAuthorization : PaymentAuthorizationBase { }

    public class PaymentAuthorizationBase 
    {
        [Parameter("uint256", "validAfter", 1)]
        public virtual BigInteger ValidAfter { get; set; }
        [Parameter("uint256", "validBefore", 2)]
        public virtual BigInteger ValidBefore { get; set; }
        [Parameter("bytes32", "nonce", 3)]
        public virtual byte[] Nonce { get; set; }
        [Parameter("uint8", "v", 4)]
        public virtual byte V { get; set; }
        [Parameter("bytes32", "r", 5)]
        public virtual byte[] R { get; set; }
        [Parameter("bytes32", "s", 6)]
        public virtual byte[] S { get; set; }
    }
}
