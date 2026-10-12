using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition
{
    public partial class PlatformConfig : PlatformConfigBase { }

    public class PlatformConfigBase 
    {
        [Parameter("address", "admin", 1)]
        public virtual string Admin { get; set; }
        [Parameter("uint48", "adminDelay", 2)]
        public virtual ulong AdminDelay { get; set; }
        [Parameter("address", "minter", 3)]
        public virtual string Minter { get; set; }
        [Parameter("address", "redeemer", 4)]
        public virtual string Redeemer { get; set; }
        [Parameter("address", "pauser", 5)]
        public virtual string Pauser { get; set; }
        [Parameter("address", "voucherSigner", 6)]
        public virtual string VoucherSigner { get; set; }
    }
}
