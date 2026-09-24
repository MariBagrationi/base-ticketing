using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TixFlow.Workers.Indexer;

[Event("Transfer")]
public class TransferEventDTO : IEventDTO
{
    [Parameter("address", "from", 1, true)]
    public string From { get; set; } = string.Empty;

    [Parameter("address", "to", 2, true)]
    public string To { get; set; } = string.Empty;

    [Parameter("uint256", "tokenId", 3, true)]
    public BigInteger TokenId { get; set; }
}
