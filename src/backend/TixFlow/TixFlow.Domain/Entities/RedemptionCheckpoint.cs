namespace TixFlow.Domain.Entities;

/// <summary>A Merkle root of every ticket redeemed at the door for one event, published on-chain.</summary>
public class RedemptionCheckpoint
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Root { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? TxHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }

    public Event Event { get; set; } = null!;
}
