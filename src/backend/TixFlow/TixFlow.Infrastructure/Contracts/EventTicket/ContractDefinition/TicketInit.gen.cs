using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition
{
    public partial class TicketInit : TicketInitBase { }

    public class TicketInitBase 
    {
        [Parameter("string", "name", 1)]
        public virtual string Name { get; set; }
        [Parameter("string", "symbol", 2)]
        public virtual string Symbol { get; set; }
        [Parameter("address", "admin", 3)]
        public virtual string Admin { get; set; }
        [Parameter("uint48", "adminDelay", 4)]
        public virtual ulong AdminDelay { get; set; }
        [Parameter("address", "organizer", 5)]
        public virtual string Organizer { get; set; }
        [Parameter("address", "minter", 6)]
        public virtual string Minter { get; set; }
        [Parameter("address", "redeemer", 7)]
        public virtual string Redeemer { get; set; }
        [Parameter("address", "pauser", 8)]
        public virtual string Pauser { get; set; }
        [Parameter("address", "voucherSigner", 9)]
        public virtual string VoucherSigner { get; set; }
        [Parameter("address", "resaleContract", 10)]
        public virtual string ResaleContract { get; set; }
        [Parameter("address", "escrow", 11)]
        public virtual string Escrow { get; set; }
        [Parameter("address", "renderer", 12)]
        public virtual string Renderer { get; set; }
        [Parameter("uint64", "eventStart", 13)]
        public virtual ulong EventStart { get; set; }
        [Parameter("uint64", "eventEnd", 14)]
        public virtual ulong EventEnd { get; set; }
        [Parameter("uint64", "unlockTime", 15)]
        public virtual ulong UnlockTime { get; set; }
        [Parameter("uint96", "royaltyBps", 16)]
        public virtual BigInteger RoyaltyBps { get; set; }
        [Parameter("tuple[]", "tiers", 17)]
        public virtual List<TierInit> Tiers { get; set; }
    }
}
