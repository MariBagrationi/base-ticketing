using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition
{
    public partial class MintVoucher : MintVoucherBase { }

    public class MintVoucherBase 
    {
        [Parameter("address", "buyer", 1)]
        public virtual string Buyer { get; set; }
        [Parameter("uint256", "tierId", 2)]
        public virtual BigInteger TierId { get; set; }
        [Parameter("uint256", "quantity", 3)]
        public virtual BigInteger Quantity { get; set; }
        [Parameter("uint256", "nonce", 4)]
        public virtual BigInteger Nonce { get; set; }
        [Parameter("uint256", "deadline", 5)]
        public virtual BigInteger Deadline { get; set; }
    }
}
