using StackExchange.Redis;

namespace TixFlow.Api.Redeem;

public class RedisChallengeStore : IChallengeStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisChallengeStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string Key(Guid ticketId) => $"redeem:challenge:{ticketId}";

    public async Task StoreAsync(Guid ticketId, string challenge, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(Key(ticketId), challenge, ttl);
    }

    public async Task<string?> ConsumeAsync(Guid ticketId)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetDeleteAsync(Key(ticketId));
        return value.IsNullOrEmpty ? null : (string?)value;
    }
}
