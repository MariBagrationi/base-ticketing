using TixFlow.Domain.Enums;

namespace TixFlow.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid TicketTierId { get; set; }
    public long? TokenId { get; set; }
    public Guid OwnerId { get; set; }
    public TicketStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }

    /// <summary>When the door redemption was included in an on-chain Merkle checkpoint.</summary>
    public DateTimeOffset? CheckpointedAt { get; set; }
    /// <summary>When the soulbound attendance badge was claimed on-chain (Redeemed log indexed).</summary>
    public DateTimeOffset? BadgeClaimedAt { get; set; }
    /// <summary>When the escrow refunded this ticket after its event was cancelled.</summary>
    public DateTimeOffset? RefundedAt { get; set; }

    public Order Order { get; set; } = null!;
    public TicketTier TicketTier { get; set; } = null!;
    public User Owner { get; set; } = null!;
    public ICollection<MintJob> MintJobs { get; set; } = [];
}
