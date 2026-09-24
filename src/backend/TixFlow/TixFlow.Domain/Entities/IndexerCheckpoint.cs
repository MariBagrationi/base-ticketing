namespace TixFlow.Domain.Entities;

public class IndexerCheckpoint
{
    public int Id { get; set; }
    public long LastProcessedBlock { get; set; }
    public string? BlockHash { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
