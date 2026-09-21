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

    public Order Order { get; set; } = null!;
    public TicketTier TicketTier { get; set; } = null!;
    public User Owner { get; set; } = null!;
}
