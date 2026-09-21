using TixFlow.Domain.Enums;

namespace TixFlow.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public Guid EventId { get; set; }
    public Guid TicketTierId { get; set; }
    public int Quantity { get; set; }
    public OrderStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public User Buyer { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public TicketTier TicketTier { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = [];
}
