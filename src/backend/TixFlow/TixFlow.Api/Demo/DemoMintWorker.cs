using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Demo;

// Stands in for TixFlow.Workers when no chain is configured, so the buyer flow completes locally.
public class DemoMintWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SubmitDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ConfirmDelay = TimeSpan.FromSeconds(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoMintWorker> _logger;

    public DemoMintWorker(IServiceScopeFactory scopeFactory, ILogger<DemoMintWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DemoMintWorker started — NFT minting is simulated locally");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "DemoMintWorker tick failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var now = DateTimeOffset.UtcNow;

        var toSubmit = await db.MintJobs
            .Where(j => j.Status == MintJobStatus.Pending && j.CreatedAt <= now - SubmitDelay)
            .ToListAsync(ct);

        foreach (var job in toSubmit)
        {
            job.Status = MintJobStatus.Submitted;
            job.TxHash = "0x" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            job.Attempts++;
            job.UpdatedAt = now;
        }

        var toConfirm = await db.MintJobs
            .Include(j => j.Ticket)
            .Where(j => j.Status == MintJobStatus.Submitted && j.UpdatedAt <= now - ConfirmDelay)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);

        if (toConfirm.Count > 0)
        {
            var nextTokenId = (await db.Tickets.MaxAsync(t => t.TokenId, ct) ?? 0) + 1;

            foreach (var job in toConfirm)
            {
                job.Status = MintJobStatus.Confirmed;
                job.UpdatedAt = now;

                if (job.Ticket.Status == TicketStatus.PendingMint)
                {
                    job.Ticket.Status = TicketStatus.Minted;
                    job.Ticket.TokenId = nextTokenId++;
                }
            }
        }

        if (toSubmit.Count > 0 || toConfirm.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo mint: {Submitted} submitted, {Confirmed} confirmed", toSubmit.Count, toConfirm.Count);
        }
    }
}
