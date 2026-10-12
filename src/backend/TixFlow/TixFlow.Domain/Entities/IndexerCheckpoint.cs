namespace TixFlow.Domain.Entities;

public class IndexerCheckpoint
{
    public int Id { get; set; }
    public long LastProcessedBlock { get; set; }
    public string? BlockHash { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Latest chain head the indexer saw. Together with <see cref="LastRpcSuccessAt"/> it lets the API
    /// report chain health without making an RPC call itself.</summary>
    public long ChainHeadBlock { get; set; }
    public DateTimeOffset? LastRpcSuccessAt { get; set; }
}
