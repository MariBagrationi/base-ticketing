using System.Collections.Concurrent;
using TixFlow.Api.Queue;

namespace TixFlow.Tests;

public class InMemoryQueueStore : IQueueStore
{
    private readonly ConcurrentDictionary<Guid, SortedList<double, Guid>> _queues = new();
    private readonly ConcurrentDictionary<string, AdmissionRecord> _admissionTokens = new();
    private readonly object _lock = new();

    public Task<(long position, long queueLength)> JoinAsync(Guid eventId, Guid userId)
    {
        lock (_lock)
        {
            var queue = _queues.GetOrAdd(eventId, _ => new SortedList<double, Guid>());

            var existingIdx = queue.Values.ToList().IndexOf(userId);
            if (existingIdx >= 0)
                return Task.FromResult(((long)existingIdx, (long)queue.Count));

            var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + queue.Count * 0.001;
            while (queue.ContainsKey(score))
                score += 0.001;

            queue.Add(score, userId);

            var pos = queue.Values.ToList().IndexOf(userId);
            return Task.FromResult(((long)pos, (long)queue.Count));
        }
    }

    public Task<bool> LeaveAsync(Guid eventId, Guid userId)
    {
        lock (_lock)
        {
            if (!_queues.TryGetValue(eventId, out var queue))
                return Task.FromResult(false);

            var entry = queue.FirstOrDefault(kv => kv.Value == userId);
            if (entry.Value == default && !queue.ContainsValue(userId))
                return Task.FromResult(false);

            return Task.FromResult(queue.Remove(entry.Key));
        }
    }

    public Task<long?> GetPositionAsync(Guid eventId, Guid userId)
    {
        lock (_lock)
        {
            if (!_queues.TryGetValue(eventId, out var queue))
                return Task.FromResult<long?>(null);

            var idx = queue.Values.ToList().IndexOf(userId);
            return Task.FromResult(idx >= 0 ? (long?)idx : null);
        }
    }

    public Task<long> GetQueueLengthAsync(Guid eventId)
    {
        lock (_lock)
        {
            return Task.FromResult(_queues.TryGetValue(eventId, out var queue) ? (long)queue.Count : 0L);
        }
    }

    public Task<QueueEntry[]> PopBatchAsync(Guid eventId, int batchSize)
    {
        lock (_lock)
        {
            if (!_queues.TryGetValue(eventId, out var queue) || queue.Count == 0)
                return Task.FromResult(Array.Empty<QueueEntry>());

            var results = new List<QueueEntry>();
            var toRemove = new List<double>();

            foreach (var kv in queue.Take(batchSize))
            {
                results.Add(new QueueEntry(kv.Value, kv.Key));
                toRemove.Add(kv.Key);
            }

            foreach (var key in toRemove)
                queue.Remove(key);

            return Task.FromResult(results.ToArray());
        }
    }

    public Task StoreAdmissionTokenAsync(string tokenId, Guid userId, Guid eventId, TimeSpan ttl)
    {
        _admissionTokens[tokenId] = new AdmissionRecord(userId, eventId, false, DateTimeOffset.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task<(bool valid, bool consumed)> ValidateAdmissionTokenAsync(string tokenId, Guid userId, Guid eventId)
    {
        if (!_admissionTokens.TryGetValue(tokenId, out var record))
            return Task.FromResult((false, false));

        if (record.UserId != userId || record.EventId != eventId)
            return Task.FromResult((false, false));

        if (record.ExpiresAt < DateTimeOffset.UtcNow)
            return Task.FromResult((false, false));

        return Task.FromResult((true, record.Consumed));
    }

    public Task ConsumeAdmissionTokenAsync(string tokenId)
    {
        if (_admissionTokens.TryGetValue(tokenId, out var record))
            _admissionTokens[tokenId] = record with { Consumed = true };
        return Task.CompletedTask;
    }

    public Task<(int batchSize, int intervalSeconds)> GetEventConfigAsync(Guid eventId, QueueOptions defaults)
    {
        return Task.FromResult((defaults.DefaultBatchSize, defaults.DefaultIntervalSeconds));
    }

    public Task<List<Guid>> GetActiveQueueEventIdsAsync()
    {
        lock (_lock)
        {
            var ids = _queues.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();
            return Task.FromResult(ids);
        }
    }

    private record AdmissionRecord(Guid UserId, Guid EventId, bool Consumed, DateTimeOffset ExpiresAt);
}
