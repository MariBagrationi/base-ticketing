namespace TixFlow.Api.Redeem;

public interface IChallengeStore
{
    Task StoreAsync(Guid ticketId, string challenge, TimeSpan ttl);
    Task<string?> ConsumeAsync(Guid ticketId);
}
