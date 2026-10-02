namespace TixFlow.Api.Queue;

public interface IQueueStore
{
    Task<(long position, long queueLength)> JoinAsync(Guid eventId, Guid userId);
    Task<bool> LeaveAsync(Guid eventId, Guid userId);
    Task<long?> GetPositionAsync(Guid eventId, Guid userId);
    Task<long> GetQueueLengthAsync(Guid eventId);
    Task<QueueEntry[]> PopBatchAsync(Guid eventId, int batchSize);
    Task StoreAdmissionTokenAsync(string tokenId, Guid userId, Guid eventId, TimeSpan ttl);
    Task<(bool valid, bool consumed)> ValidateAdmissionTokenAsync(string tokenId, Guid userId, Guid eventId);
    Task ConsumeAdmissionTokenAsync(string tokenId);
    Task StoreLatestAdmissionAsync(Guid eventId, Guid userId, string token, string tokenId, TimeSpan ttl);
    Task<(string token, string tokenId)?> GetLatestAdmissionAsync(Guid eventId, Guid userId);
    Task<(int batchSize, int intervalSeconds)> GetEventConfigAsync(Guid eventId, QueueOptions defaults);
    Task<List<Guid>> GetActiveQueueEventIdsAsync();
}

public record QueueEntry(Guid UserId, double Score);
