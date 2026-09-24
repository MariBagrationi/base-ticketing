using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

public class MintWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContractMintClient _mintClient;
    private readonly INonceService _nonceService;
    private readonly MintOptions _options;
    private readonly ILogger<MintWorker> _logger;

    public MintWorker(
        IServiceScopeFactory scopeFactory,
        IContractMintClient mintClient,
        INonceService nonceService,
        IOptions<MintOptions> options,
        ILogger<MintWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _mintClient = mintClient;
        _nonceService = nonceService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _nonceService.InitializeAsync();
        _logger.LogInformation("MintWorker started, polling every {Interval}s", _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error in MintWorker loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    public async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var pendingJobs = await db.MintJobs
            .Where(j => j.Status == MintJobStatus.Pending && j.Attempts < _options.MaxAttempts)
            .OrderBy(j => j.CreatedAt)
            .Take(_options.MaxBatchSize)
            .Include(j => j.Ticket)
                .ThenInclude(t => t.Owner)
            .ToListAsync(ct);

        if (pendingJobs.Count == 0) return;

        if (pendingJobs.Count > 1)
        {
            await ProcessBatchAsync(db, pendingJobs, ct);
        }
        else
        {
            await ProcessSingleAsync(db, pendingJobs[0], ct);
        }
    }

    private async Task ProcessSingleAsync(TixFlowDbContext db, Domain.Entities.MintJob job, CancellationToken ct)
    {
        var ownerAddress = job.Ticket.Owner.WalletAddress;
        var nonce = _nonceService.GetNextNonce();

        try
        {
            _logger.LogInformation("Minting ticket {TicketId} to {Address} with nonce {Nonce}",
                job.TicketId, ownerAddress, nonce);

            var txHash = await _mintClient.MintAsync(ownerAddress, nonce);

            job.Status = MintJobStatus.Submitted;
            job.TxHash = txHash;
            job.Nonce = nonce;
            job.UpdatedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation("Submitted mint tx {TxHash} for ticket {TicketId}", txHash, job.TicketId);
        }
        catch (Exception ex)
        {
            job.Attempts++;
            job.LastError = Truncate(ex.Message, 500);
            job.UpdatedAt = DateTimeOffset.UtcNow;

            if (job.Attempts >= _options.MaxAttempts)
            {
                job.Status = MintJobStatus.Failed;
                _logger.LogError(ex, "MintJob {JobId} failed permanently after {Attempts} attempts", job.Id, job.Attempts);
            }
            else
            {
                _logger.LogWarning(ex, "MintJob {JobId} attempt {Attempt} failed, will retry", job.Id, job.Attempts);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ProcessBatchAsync(TixFlowDbContext db, List<Domain.Entities.MintJob> jobs, CancellationToken ct)
    {
        var addresses = jobs.Select(j => j.Ticket.Owner.WalletAddress).ToArray();
        var nonce = _nonceService.GetNextNonce();
        var now = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation("Batch minting {Count} tickets with nonce {Nonce}", jobs.Count, nonce);

            var txHash = await _mintClient.MintBatchAsync(addresses, nonce);

            foreach (var job in jobs)
            {
                job.Status = MintJobStatus.Submitted;
                job.TxHash = txHash;
                job.Nonce = nonce;
                job.UpdatedAt = now;
            }

            _logger.LogInformation("Submitted batch mint tx {TxHash} for {Count} tickets", txHash, jobs.Count);
        }
        catch (Exception ex)
        {
            foreach (var job in jobs)
            {
                job.Attempts++;
                job.LastError = Truncate(ex.Message, 500);
                job.UpdatedAt = now;

                if (job.Attempts >= _options.MaxAttempts)
                    job.Status = MintJobStatus.Failed;
            }

            _logger.LogError(ex, "Batch mint failed for {Count} jobs", jobs.Count);
        }

        await db.SaveChangesAsync(ct);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
