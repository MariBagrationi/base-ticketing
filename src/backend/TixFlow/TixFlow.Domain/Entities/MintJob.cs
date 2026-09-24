using TixFlow.Domain.Enums;

namespace TixFlow.Domain.Entities;

public class MintJob
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public MintJobStatus Status { get; set; }
    public string? TxHash { get; set; }
    public long? Nonce { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
