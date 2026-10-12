namespace TixFlow.Domain.Entities;

public class TicketTier
{
    public const int DefaultMaxPerWallet = 4;

    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceUsdc { get; set; }
    public int TotalSupply { get; set; }

    /// <summary>Tier identifier inside the event's ticket contract (1-based, unique per event).</summary>
    public int OnChainTierId { get; set; }
    /// <summary>Most tickets one wallet may buy in this tier. Enforced by the API and again by the contract.</summary>
    public int MaxPerWallet { get; set; } = DefaultMaxPerWallet;

    public Event Event { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
