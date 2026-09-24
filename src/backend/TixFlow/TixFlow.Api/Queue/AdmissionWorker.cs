using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace TixFlow.Api.Queue;

public class AdmissionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<QueueHub> _hubContext;
    private readonly QueueOptions _options;
    private readonly ILogger<AdmissionWorker> _logger;

    public AdmissionWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<QueueHub> hubContext,
        IOptions<QueueOptions> options,
        ILogger<AdmissionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AdmissionWorker started. Default batch={Batch}, interval={Interval}s",
            _options.DefaultBatchSize, _options.DefaultIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllQueuesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "AdmissionWorker tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.DefaultIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessAllQueuesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueStore>();
        var tokenService = scope.ServiceProvider.GetRequiredService<AdmissionTokenService>();

        var eventIds = await queueService.GetActiveQueueEventIdsAsync();

        foreach (var eventId in eventIds)
        {
            if (ct.IsCancellationRequested) break;

            var (batchSize, _) = await queueService.GetEventConfigAsync(eventId, _options);
            var batch = await queueService.PopBatchAsync(eventId, batchSize);

            if (batch.Length == 0)
                continue;

            _logger.LogInformation("Admitting {Count} users for event {EventId}", batch.Length, eventId);

            var ttl = TimeSpan.FromMinutes(_options.AdmissionTokenTtlMinutes);

            foreach (var entry in batch)
            {
                var userId = entry.UserId;

                var (token, tokenId) = tokenService.GenerateAdmissionToken(userId, eventId);
                await queueService.StoreAdmissionTokenAsync(tokenId, userId, eventId, ttl);

                try
                {
                    var group = QueueHub.UserGroup(eventId.ToString(), userId.ToString());
                    await _hubContext.Clients.Group(group).SendAsync("Admitted", new
                    {
                        eventId = eventId.ToString(),
                        admissionToken = token,
                        expiresInSeconds = _options.AdmissionTokenTtlMinutes * 60
                    }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to push admission notification to user {UserId}", userId);
                }
            }

            await BroadcastPositionUpdatesAsync(queueService, eventId, ct);
        }
    }

    private async Task BroadcastPositionUpdatesAsync(IQueueStore queueService, Guid eventId, CancellationToken ct)
    {
        try
        {
            var remaining = await queueService.GetQueueLengthAsync(eventId);
            var eventIdStr = eventId.ToString();
            var eventGroup = QueueHub.EventGroup(eventIdStr);

            await _hubContext.Clients.Group(eventGroup).SendAsync("QueueUpdate", new
            {
                eventId = eventIdStr,
                queueLength = remaining,
                batchSize = _options.DefaultBatchSize,
                intervalSeconds = _options.DefaultIntervalSeconds
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast position updates for event {EventId}", eventId);
        }
    }
}
