namespace TixFlow.Domain.Entities;

/// <summary>Append-only record of privileged actions: who did what to whom, and when.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorWallet { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
