using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TixFlow.Infrastructure.Chain;

namespace TixFlow.Workers;

/// <summary>Runs <see cref="TickAsync"/> on a fixed interval and survives any failure of a single tick.</summary>
public abstract class PollingWorker : BackgroundService
{
    private readonly ILogger _logger;

    protected PollingWorker(ILogger logger)
    {
        _logger = logger;
    }

    protected abstract TimeSpan Interval { get; }

    public abstract Task TickAsync(CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Worker} started, polling every {Seconds}s", GetType().Name, Interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (ChainRpc.IsTransportFailure(ex))
                    _logger.LogWarning("{Worker}: chain unreachable, will retry ({Message})", GetType().Name, ex.Message);
                else
                    _logger.LogError(ex, "{Worker} tick failed", GetType().Name);
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
