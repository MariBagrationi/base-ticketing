using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers.Indexer;

public class ChainIndexer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWeb3Provider _web3Provider;
    private readonly IndexerOptions _options;
    private readonly MintOptions _mintOptions;
    private readonly ILogger<ChainIndexer> _logger;

    public ChainIndexer(
        IServiceScopeFactory scopeFactory,
        IWeb3Provider web3Provider,
        IOptions<IndexerOptions> options,
        IOptions<MintOptions> mintOptions,
        ILogger<ChainIndexer> logger)
    {
        _scopeFactory = scopeFactory;
        _web3Provider = web3Provider;
        _options = options.Value;
        _mintOptions = mintOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChainIndexer started, polling every {Interval}s with {Conf} confirmation blocks",
            _options.PollIntervalSeconds, _options.ConfirmationBlocks);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error in ChainIndexer loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    public async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var checkpoint = await GetOrCreateCheckpointAsync(db, ct);

        if (await DetectAndHandleReorgAsync(db, checkpoint, ct))
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        var headBlock = await _web3Provider.GetBlockNumberAsync();
        var safeHead = headBlock - _options.ConfirmationBlocks;

        if (safeHead <= checkpoint.LastProcessedBlock)
            return;

        var fromBlock = checkpoint.LastProcessedBlock + 1;
        var toBlock = safeHead;

        _logger.LogInformation("Processing blocks {From} to {To}", fromBlock, toBlock);

        var web3 = _web3Provider.GetWeb3();
        await ProcessTransferLogsAsync(db, web3, fromBlock, toBlock, ct);
        await HandleStuckTransactionsAsync(db, ct);

        var blockHash = await _web3Provider.GetBlockHashAtAsync(toBlock);
        checkpoint.LastProcessedBlock = toBlock;
        checkpoint.BlockHash = blockHash;
        checkpoint.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Checkpoint advanced to block {Block}", toBlock);
    }

    private async Task<IndexerCheckpoint> GetOrCreateCheckpointAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var checkpoint = await db.IndexerCheckpoints.FirstOrDefaultAsync(ct);
        if (checkpoint is not null)
            return checkpoint;

        checkpoint = new IndexerCheckpoint
        {
            Id = 1,
            LastProcessedBlock = _options.ContractDeployBlock > 0
                ? _options.ContractDeployBlock - 1
                : 0,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IndexerCheckpoints.Add(checkpoint);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Initialized checkpoint at block {Block}", checkpoint.LastProcessedBlock);
        return checkpoint;
    }

    public async Task<bool> DetectAndHandleReorgAsync(TixFlowDbContext db, IndexerCheckpoint checkpoint, CancellationToken ct)
    {
        if (checkpoint.BlockHash is null)
            return false;

        var chainHash = await _web3Provider.GetBlockHashAtAsync(checkpoint.LastProcessedBlock);

        if (chainHash is null)
        {
            _logger.LogWarning("Block {Block} not found on chain — possible deep reorg", checkpoint.LastProcessedBlock);
            await RewindCheckpointAsync(db, checkpoint, ct);
            return true;
        }

        if (chainHash == checkpoint.BlockHash)
            return false;

        _logger.LogWarning("Reorg detected at block {Block}: stored hash {Stored}, chain hash {Chain}",
            checkpoint.LastProcessedBlock, checkpoint.BlockHash, chainHash);

        await RewindCheckpointAsync(db, checkpoint, ct);
        return true;
    }

    private async Task RewindCheckpointAsync(TixFlowDbContext db, IndexerCheckpoint checkpoint, CancellationToken ct)
    {
        var rewindTo = Math.Max(
            checkpoint.LastProcessedBlock - _options.ReorgRewindBlocks,
            _options.ContractDeployBlock > 0 ? _options.ContractDeployBlock - 1 : 0);

        _logger.LogWarning("Rewinding checkpoint from {From} to {To}", checkpoint.LastProcessedBlock, rewindTo);

        var affectedJobs = await db.MintJobs
            .Include(j => j.Ticket)
            .Where(j => j.Status == MintJobStatus.Confirmed)
            .Where(j => j.UpdatedAt >= DateTimeOffset.UtcNow.AddMinutes(-10))
            .ToListAsync(ct);

        var reverted = 0;
        foreach (var job in affectedJobs)
        {
            job.Status = MintJobStatus.Submitted;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            job.Ticket.Status = TicketStatus.PendingMint;
            job.Ticket.TokenId = null;
            reverted++;
        }

        checkpoint.LastProcessedBlock = rewindTo;
        checkpoint.BlockHash = null;
        checkpoint.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogWarning("Reverted {Count} confirmed MintJobs back to Submitted", reverted);
    }

    private async Task ProcessTransferLogsAsync(TixFlowDbContext db, IWeb3 web3, long fromBlock, long toBlock, CancellationToken ct)
    {
        var transferEvent = web3.Eth.GetEvent<TransferEventDTO>(_mintOptions.ContractAddress);
        var filter = transferEvent.CreateFilterInput(
            new BlockParameter((ulong)fromBlock),
            new BlockParameter((ulong)toBlock));

        var logs = await transferEvent.GetAllChangesAsync(filter);

        var mintLogs = logs.Where(l =>
            l.Event.From == "0x0000000000000000000000000000000000000000").ToList();

        if (mintLogs.Count == 0)
            return;

        _logger.LogInformation("Found {Count} mint Transfer events in blocks {From}-{To}",
            mintLogs.Count, fromBlock, toBlock);

        var txHashes = mintLogs.Select(l => l.Log.TransactionHash).Distinct().ToList();
        var submittedJobs = await db.MintJobs
            .Include(j => j.Ticket)
            .Where(j => j.TxHash != null && txHashes.Contains(j.TxHash))
            .Where(j => j.Status == MintJobStatus.Submitted)
            .ToListAsync(ct);

        var jobsByTxHash = submittedJobs
            .GroupBy(j => j.TxHash!)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var log in mintLogs)
        {
            var txHash = log.Log.TransactionHash;
            var tokenId = (long)log.Event.TokenId;

            if (!jobsByTxHash.TryGetValue(txHash, out var matchingJobs))
            {
                _logger.LogWarning(
                    "Transfer event for tokenId {TokenId} in tx {TxHash} has no matching MintJob — possibly a manual mint",
                    tokenId, txHash);
                continue;
            }

            var job = matchingJobs.FirstOrDefault(j => j.Status == MintJobStatus.Submitted);
            if (job is null)
                continue;

            job.Status = MintJobStatus.Confirmed;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            job.Ticket.TokenId = tokenId;
            job.Ticket.Status = TicketStatus.Minted;

            matchingJobs.Remove(job);

            _logger.LogInformation("Confirmed MintJob {JobId} — ticket {TicketId} got tokenId {TokenId}",
                job.Id, job.TicketId, tokenId);
        }
    }

    public async Task HandleStuckTransactionsAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-_options.StuckTxTimeoutMinutes);

        var stuckJobs = await db.MintJobs
            .Where(j => j.Status == MintJobStatus.Submitted)
            .Where(j => j.UpdatedAt < cutoff)
            .Where(j => j.Attempts < _mintOptions.MaxAttempts)
            .ToListAsync(ct);

        if (stuckJobs.Count == 0)
            return;

        foreach (var job in stuckJobs)
        {
            job.Status = MintJobStatus.Pending;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            job.LastError = "Stuck transaction timeout — resubmitting";
        }

        _logger.LogWarning("Reset {Count} stuck Submitted MintJobs back to Pending for retry", stuckJobs.Count);
    }
}
