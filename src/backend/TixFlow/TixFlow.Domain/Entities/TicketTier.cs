namespace TixFlow.Domain.Entities;

public class TicketTier
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceUsdc { get; set; }
    public int TotalSupply { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
