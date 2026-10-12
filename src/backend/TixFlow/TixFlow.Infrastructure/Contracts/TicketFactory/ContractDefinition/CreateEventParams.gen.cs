using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition
{
    public partial class CreateEventParams : CreateEventParamsBase { }

    public class CreateEventParamsBase 
    {
        [Parameter("bytes32", "eventId", 1)]
        public virtual byte[] EventId { get; set; }
        [Parameter("string", "name", 2)]
        public virtual string Name { get; set; }
        [Parameter("string", "symbol", 3)]
        public virtual string Symbol { get; set; }
        [Parameter("address", "organizer", 4)]
        public virtual string Organizer { get; set; }
        [Parameter("uint64", "eventStart", 5)]
        public virtual ulong EventStart { get; set; }
        [Parameter("uint64", "eventEnd", 6)]
        public virtual ulong EventEnd { get; set; }
        [Parameter("uint64", "unlockTime", 7)]
        public virtual ulong UnlockTime { get; set; }
        [Parameter("uint96", "royaltyBps", 8)]
        public virtual BigInteger RoyaltyBps { get; set; }
        [Parameter("tuple[]", "tiers", 9)]
        public virtual List<TierInit> Tiers { get; set; }
    }
}
