namespace TixFlow.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public Guid OrganizerId { get; set; }

    /// <summary>Address of the event's EventTicket clone, filled by the indexer from the factory's EventCreated log.</summary>
    public string? ContractAddress { get; set; }
    public string? DeployTxHash { get; set; }
    public DateTimeOffset? DeploySubmittedAt { get; set; }

    public DateTimeOffset? CancelRequestedAt { get; set; }
    public string? CancelTxHash { get; set; }
    /// <summary>Set when the escrow's EventCancelled log is indexed (or immediately when no chain is configured).</summary>
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? EscrowReleasedAt { get; set; }

    public User Organizer { get; set; } = null!;
    public ICollection<TicketTier> TicketTiers { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}
