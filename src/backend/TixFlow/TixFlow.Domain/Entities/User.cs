namespace TixFlow.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>When the local-chain faucet sent this wallet test USDC. Null means not funded yet.</summary>
    public DateTimeOffset? FaucetFundedAt { get; set; }

    public ICollection<Event> OrganizedEvents { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Ticket> OwnedTickets { get; set; } = [];
}
