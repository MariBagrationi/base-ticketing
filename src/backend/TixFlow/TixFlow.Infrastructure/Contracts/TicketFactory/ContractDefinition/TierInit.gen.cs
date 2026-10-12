using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition
{
    public partial class TierInit : TierInitBase { }

    public class TierInitBase 
    {
        [Parameter("uint256", "tierId", 1)]
        public virtual BigInteger TierId { get; set; }
        [Parameter("string", "name", 2)]
        public virtual string Name { get; set; }
        [Parameter("uint256", "price", 3)]
        public virtual BigInteger Price { get; set; }
        [Parameter("uint256", "supplyCap", 4)]
        public virtual BigInteger SupplyCap { get; set; }
        [Parameter("uint256", "maxPerWallet", 5)]
        public virtual BigInteger MaxPerWallet { get; set; }
    }
}
