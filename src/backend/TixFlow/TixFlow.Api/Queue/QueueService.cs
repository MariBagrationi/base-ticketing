using StackExchange.Redis;

namespace TixFlow.Api.Queue;

public class QueueService : IQueueStore
{
    private readonly IConnectionMultiplexer _redis;

    public QueueService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string QueueKey(Guid eventId) => $"queue:{eventId}";
    private static string AdmissionTokenKey(string tokenId) => $"admission:{tokenId}";
    private static string EventConfigKey(Guid eventId) => $"queue:{eventId}:config";

    public async Task<(long position, long queueLength)> JoinAsync(Guid eventId, Guid userId)
    {
        var db = _redis.GetDatabase();
        var key = QueueKey(eventId);
        var member = userId.ToString();

        var rank = await db.SortedSetRankAsync(key, member);
        if (rank.HasValue)
            return (rank.Value, await db.SortedSetLengthAsync(key));

        var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.SortedSetAddAsync(key, member, score);

        rank = await db.SortedSetRankAsync(key, member);
        var length = await db.SortedSetLengthAsync(key);
        return (rank ?? 0, length);
    }

    public async Task<bool> LeaveAsync(Guid eventId, Guid userId)
    {
        var db = _redis.GetDatabase();
        return await db.SortedSetRemoveAsync(QueueKey(eventId), userId.ToString());
    }

    public async Task<long?> GetPositionAsync(Guid eventId, Guid userId)
    {
        var db = _redis.GetDatabase();
        return await db.SortedSetRankAsync(QueueKey(eventId), userId.ToString());
    }

    public async Task<long> GetQueueLengthAsync(Guid eventId)
    {
        var db = _redis.GetDatabase();
        return await db.SortedSetLengthAsync(QueueKey(eventId));
    }

    public async Task<QueueEntry[]> PopBatchAsync(Guid eventId, int batchSize)
    {
        var db = _redis.GetDatabase();
        var entries = await db.SortedSetPopAsync(QueueKey(eventId), batchSize, Order.Ascending);
        return entries.Select(e => new QueueEntry(Guid.Parse(e.Element.ToString()), e.Score)).ToArray();
    }

    public async Task StoreAdmissionTokenAsync(string tokenId, Guid userId, Guid eventId, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        var key = AdmissionTokenKey(tokenId);
        await db.HashSetAsync(key,
        [
            new HashEntry("userId", userId.ToString()),
            new HashEntry("eventId", eventId.ToString()),
            new HashEntry("consumed", "false")
        ]);
        await db.KeyExpireAsync(key, ttl);
    }

    public async Task<(bool valid, bool consumed)> ValidateAdmissionTokenAsync(string tokenId, Guid userId, Guid eventId)
    {
        var db = _redis.GetDatabase();
        var key = AdmissionTokenKey(tokenId);
        var entries = await db.HashGetAllAsync(key);

        if (entries.Length == 0)
            return (false, false);

        var storedUserId = entries.FirstOrDefault(e => e.Name == "userId").Value.ToString();
        var storedEventId = entries.FirstOrDefault(e => e.Name == "eventId").Value.ToString();
        var consumed = entries.FirstOrDefault(e => e.Name == "consumed").Value.ToString();

        if (storedUserId != userId.ToString() || storedEventId != eventId.ToString())
            return (false, false);

        return (true, consumed == "true");
    }

    public async Task ConsumeAdmissionTokenAsync(string tokenId)
    {
        var db = _redis.GetDatabase();
        var key = AdmissionTokenKey(tokenId);
        await db.HashSetAsync(key, "consumed", "true", When.Exists);
    }

    public async Task<(int batchSize, int intervalSeconds)> GetEventConfigAsync(Guid eventId, QueueOptions defaults)
    {
        var db = _redis.GetDatabase();
        var entries = await db.HashGetAllAsync(EventConfigKey(eventId));

        if (entries.Length == 0)
            return (defaults.DefaultBatchSize, defaults.DefaultIntervalSeconds);

        var batchSize = (int?)entries.FirstOrDefault(e => e.Name == "batchSize").Value ?? defaults.DefaultBatchSize;
        var interval = (int?)entries.FirstOrDefault(e => e.Name == "intervalSeconds").Value ?? defaults.DefaultIntervalSeconds;
        return (batchSize, interval);
    }

    public async Task<List<Guid>> GetActiveQueueEventIdsAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.KeysAsync(pattern: "queue:*", pageSize: 100);
        var eventIds = new List<Guid>();

        await foreach (var key in keys)
        {
            var keyStr = key.ToString();
            if (keyStr.Contains(":config") || keyStr.Contains("admission:"))
                continue;

            var parts = keyStr.Split(':');
            if (parts.Length == 2 && Guid.TryParse(parts[1], out var eventId))
                eventIds.Add(eventId);
        }

        return eventIds;
    }
}
