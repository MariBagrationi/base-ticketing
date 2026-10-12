namespace TixFlow.Domain.Entities;

/// <summary>One indexed contract log, kept so the UI can show verifiable on-chain history per event.</summary>
public class ChainActivity
{
    public long Id { get; set; }
    public Guid? EventId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string TxHash { get; set; } = string.Empty;
    public int LogIndex { get; set; }
    public long BlockNumber { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
