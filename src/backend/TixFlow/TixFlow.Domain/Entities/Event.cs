namespace TixFlow.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public Guid OrganizerId { get; set; }
    public string? ContractAddress { get; set; }

    public User Organizer { get; set; } = null!;
    public ICollection<TicketTier> TicketTiers { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}
