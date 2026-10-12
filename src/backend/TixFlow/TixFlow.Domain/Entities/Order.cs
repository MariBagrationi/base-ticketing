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

    // Buyer-signed EIP-3009 authorisation to pull the order total into escrow. Stored at confirm time and
    // relayed on-chain later by the mint worker, so checkout never waits for the chain.
    public long? PaymentValidAfter { get; set; }
    public long? PaymentValidBefore { get; set; }
    public string? PaymentNonce { get; set; }
    public string? PaymentSignature { get; set; }

    public bool HasPaymentAuthorization => PaymentSignature is not null;

    public User Buyer { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public TicketTier TicketTier { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = [];
}
