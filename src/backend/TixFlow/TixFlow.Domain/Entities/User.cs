namespace TixFlow.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Event> OrganizedEvents { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Ticket> OwnedTickets { get; set; } = [];
}
