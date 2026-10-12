namespace TixFlow.Domain.Enums;

public enum Role
{
    /// <summary>Buyer: queues, checks out, holds and resells own tickets.</summary>
    User,
    /// <summary>Venue checker: scans tickets and sees check-in counts for assigned events.</summary>
    Staff,
    /// <summary>Platform operator: everything, including events, roles and cancellations.</summary>
    Admin
}
