namespace TixFlow.Domain.Entities;

/// <summary>Limits a staff member to scanning tickets for one event. No assignments means all events.</summary>
public class StaffAssignment
{
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
}
