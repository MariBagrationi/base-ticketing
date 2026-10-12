using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition;
using TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition;
using TixFlow.Infrastructure.Contracts.PriceCappedResale.ContractDefinition;
using TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers.Indexer;

/// <summary>Where a log came from. Identifies it uniquely and makes re-indexing idempotent.</summary>
public record LogContext(string Contract, string TxHash, int LogIndex, long BlockNumber);

/// <summary>
/// Mirrors on-chain state into Postgres. The chain is the source of truth for what was minted, redeemed,
/// refunded or cancelled; this service is the only thing that writes those facts to the database.
/// </summary>
public class ChainIndexer : BackgroundService
{
    private const string ZeroAddress = "0x0000000000000000000000000000000000000000";
    private const int MaxBlocksPerTick = 1000;
    private const byte RejectionNonceUsed = 1;

    private static readonly string[] RejectionReasons =
        ["None", "NonceUsed", "ZeroAddress", "ZeroQuantity", "UnknownTier", "SupplyCapExceeded", "MaxPerWalletExceeded"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWeb3Provider _web3Provider;
    private readonly IndexerOptions _options;
    private readonly MintOptions _mintOptions;
    private readonly ChainOptions _chain;
    private readonly ILogger<ChainIndexer> _logger;

    public ChainIndexer(
        IServiceScopeFactory scopeFactory,
        IWeb3Provider web3Provider,
        IOptions<IndexerOptions> options,
        IOptions<MintOptions> mintOptions,
        IOptions<ChainOptions> chainOptions,
        ILogger<ChainIndexer> logger)
    {
        _scopeFactory = scopeFactory;
        _web3Provider = web3Provider;
        _options = options.Value;
        _mintOptions = mintOptions.Value;
        _chain = chainOptions.Value;
        _logger = logger;
    }

    private long DeployBlock => _options.ContractDeployBlock > 0 ? _options.ContractDeployBlock : _chain.Contracts.DeployBlock;

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
                if (ChainRpc.IsTransportFailure(ex))
                    _logger.LogWarning("ChainIndexer: chain unreachable, will retry ({Message})", ex.Message);
                else
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

        // Heartbeat: lets the API report chain health without ever calling the chain itself.
        checkpoint.ChainHeadBlock = headBlock;
        checkpoint.LastRpcSuccessAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var safeHead = headBlock - _options.ConfirmationBlocks;
        if (safeHead <= checkpoint.LastProcessedBlock)
        {
            await HandleStuckTransactionsAsync(db, ct);
            await db.SaveChangesAsync(ct);
            return;
        }

        var fromBlock = checkpoint.LastProcessedBlock + 1;
        var toBlock = Math.Min(safeHead, fromBlock + MaxBlocksPerTick - 1);

        // Pass 1: factory logs, so contracts created in this range are known before their own logs are read.
        if (!string.IsNullOrWhiteSpace(_chain.Contracts.TicketFactory))
        {
            var factoryLogs = await _web3Provider.GetLogsAsync(fromBlock, toBlock, [_chain.Contracts.TicketFactory]);
            await ApplyLogsAsync(db, factoryLogs, ct);
        }

        // Pass 2: every event contract, the escrow and the marketplace.
        var addresses = await db.Events
            .Where(e => e.ContractAddress != null)
            .Select(e => e.ContractAddress!)
            .ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(_chain.Contracts.EventEscrow)) addresses.Add(_chain.Contracts.EventEscrow);
        if (!string.IsNullOrWhiteSpace(_chain.Contracts.PriceCappedResale)) addresses.Add(_chain.Contracts.PriceCappedResale);

        var logs = await _web3Provider.GetLogsAsync(fromBlock, toBlock, addresses);
        await ApplyLogsAsync(db, logs, ct);
        await HandleStuckTransactionsAsync(db, ct);

        var blockHash = await _web3Provider.GetBlockHashAtAsync(toBlock);
        checkpoint.LastProcessedBlock = toBlock;
        checkpoint.BlockHash = blockHash;
        checkpoint.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        if (logs.Length > 0)
            _logger.LogInformation("Indexed {Count} logs in blocks {From}-{To}", logs.Length, fromBlock, toBlock);
    }

    private async Task<IndexerCheckpoint> GetOrCreateCheckpointAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var checkpoint = await db.IndexerCheckpoints.FirstOrDefaultAsync(ct);
        if (checkpoint is not null)
            return checkpoint;

        checkpoint = new IndexerCheckpoint
        {
            Id = 1,
            LastProcessedBlock = DeployBlock > 0 ? DeployBlock - 1 : 0,
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
            DeployBlock > 0 ? DeployBlock - 1 : 0);

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

        // Activity from the orphaned blocks is dropped; it is re-recorded if the logs appear again.
        var orphaned = await db.ChainActivities.Where(a => a.BlockNumber > rewindTo).ToListAsync(ct);
        db.ChainActivities.RemoveRange(orphaned);

        checkpoint.LastProcessedBlock = rewindTo;
        checkpoint.BlockHash = null;
        checkpoint.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogWarning("Reverted {Count} confirmed MintJobs back to Submitted", reverted);
    }

    // ---------------------------------------------------------------------
    // Log handling
    // ---------------------------------------------------------------------

    public async Task ApplyLogsAsync(TixFlowDbContext db, IEnumerable<FilterLog> logs, CancellationToken ct)
    {
        foreach (var log in logs.OrderBy(l => l.BlockNumber.Value).ThenBy(l => l.LogIndex.Value))
        {
            var ctx = new LogContext(
                log.Address.ToLowerInvariant(), log.TransactionHash, (int)log.LogIndex.Value, (long)log.BlockNumber.Value);

            if (log.IsLogForEvent<EventCreatedEventDTO>())
                await ApplyEventCreatedAsync(db, log.DecodeEvent<EventCreatedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<MintedEventDTO>())
                await ApplyMintedAsync(db, log.DecodeEvent<MintedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<OrderRejectedEventDTO>())
                await ApplyOrderRejectedAsync(db, log.DecodeEvent<OrderRejectedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<TransferEventDTO>())
                await ApplyTransferAsync(db, log.DecodeEvent<TransferEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<RedeemedEventDTO>())
                await ApplyRedeemedAsync(db, log.DecodeEvent<RedeemedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<RedemptionsCheckpointedEventDTO>())
                await ApplyCheckpointedAsync(db, log.DecodeEvent<RedemptionsCheckpointedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<PaymentDepositedEventDTO>())
                await ApplyPaymentDepositedAsync(db, log.DecodeEvent<PaymentDepositedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<EscrowReleasedEventDTO>())
                await ApplyEscrowReleasedAsync(db, log.DecodeEvent<EscrowReleasedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<EventCancelledEventDTO>())
                await ApplyEventCancelledAsync(db, log.DecodeEvent<EventCancelledEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<RefundedEventDTO>())
                await ApplyRefundedAsync(db, log.DecodeEvent<RefundedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<ListedEventDTO>())
                await ApplyListedAsync(db, log.DecodeEvent<ListedEventDTO>().Event, ctx, ct);
            else if (log.IsLogForEvent<SoldEventDTO>())
                await RecordAsync(db, null, "ResaleSold", ctx,
                    $"Resale settled for {Usdc(log.DecodeEvent<SoldEventDTO>().Event.Price)} USDC", ct);
            else
                continue;

            // Saved per log so later handlers in the same batch can query what earlier ones wrote.
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task ApplyEventCreatedAsync(TixFlowDbContext db, EventCreatedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var eventId = ChainIds.EventIdFromBytes(e.EventId);
        var evt = eventId is null ? null : await db.Events.FirstOrDefaultAsync(x => x.Id == eventId, ct);
        if (evt is null)
        {
            _logger.LogWarning("EventCreated for unknown event in tx {TxHash}", ctx.TxHash);
            return;
        }

        evt.ContractAddress = e.Ticket.ToLowerInvariant();
        evt.DeployTxHash ??= ctx.TxHash;
        await RecordAsync(db, evt.Id, "EventCreated", ctx, $"Ticket contract deployed at {Short(e.Ticket)}", ct);
        _logger.LogInformation("Event {EventId} deployed at {Address}", evt.Id, evt.ContractAddress);
    }

    public async Task ApplyMintedAsync(TixFlowDbContext db, MintedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, ctx.Contract, ct);
        if (evt is null) return;

        var orderId = ChainIds.OrderIdFromNonce(e.Nonce);
        var order = orderId is null
            ? null
            : await db.Orders
                .Include(o => o.Tickets).ThenInclude(t => t.MintJobs)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.EventId == evt.Id, ct);

        var first = (long)e.FirstTokenId;
        var quantity = (int)e.Quantity;
        var range = quantity == 1 ? $"#{first}" : $"#{first}-#{first + quantity - 1}";

        if (order is null)
        {
            _logger.LogWarning("Minted log in tx {TxHash} has no matching order (manual mint?)", ctx.TxHash);
            await RecordAsync(db, evt.Id, "Minted", ctx, $"Minted {range} to {Short(e.Buyer)} (no matching order)", ct);
            return;
        }

        // Token IDs are consecutive; pairing them with tickets in a stable order keeps re-indexing deterministic.
        var tickets = order.Tickets.OrderBy(t => t.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < Math.Min(quantity, tickets.Count); i++)
        {
            var ticket = tickets[i];
            ticket.TokenId = first + i;
            if (ticket.Status == TicketStatus.PendingMint)
                ticket.Status = TicketStatus.Minted;

            foreach (var job in ticket.MintJobs.Where(j => j.Status != MintJobStatus.Confirmed))
            {
                job.Status = MintJobStatus.Confirmed;
                job.TxHash = ctx.TxHash;
                job.UpdatedAt = now;
            }
        }

        await RecordAsync(db, evt.Id, "Minted", ctx, $"Minted {range} to {Short(e.Buyer)}", ct);
        _logger.LogInformation("Order {OrderId} minted as tokens {Range} in tx {TxHash}", order.Id, range, ctx.TxHash);
    }

    public async Task ApplyOrderRejectedAsync(TixFlowDbContext db, OrderRejectedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        // NonceUsed means the order was already minted by the other path, which is not a failure.
        if (e.Reason == RejectionNonceUsed) return;

        var evt = await EventByContractAsync(db, ctx.Contract, ct);
        var orderId = ChainIds.OrderIdFromNonce(e.Nonce);
        if (evt is null || orderId is null) return;

        var reason = e.Reason < RejectionReasons.Length ? RejectionReasons[e.Reason] : $"Reason {e.Reason}";
        var jobs = await db.MintJobs
            .Where(j => j.Ticket.OrderId == orderId && j.Status != MintJobStatus.Confirmed)
            .ToListAsync(ct);
        foreach (var job in jobs)
        {
            job.Status = MintJobStatus.Failed;
            job.LastError = $"Rejected by contract: {reason}";
            job.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await RecordAsync(db, evt.Id, "OrderRejected", ctx, $"Order for {Short(e.Buyer)} rejected on-chain: {reason}", ct);
    }

    public async Task ApplyTransferAsync(TixFlowDbContext db, TransferEventDTO e, LogContext ctx, CancellationToken ct)
    {
        // Mints are handled through the Minted log. A move into the marketplace is an escrowed listing:
        // the seller still owns the ticket until it is sold.
        if (e.From.IsTheSameAddress(ZeroAddress) || e.To.IsTheSameAddress(_chain.Contracts.PriceCappedResale))
            return;

        var evt = await EventByContractAsync(db, ctx.Contract, ct);
        if (evt is null) return;

        var tokenId = (long)e.TokenId;
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketTier.EventId == evt.Id && t.TokenId == tokenId, ct);
        if (ticket is null) return;

        var wallet = e.To.ToLowerInvariant();
        var owner = await db.Users.FirstOrDefaultAsync(u => u.WalletAddress == wallet, ct);
        if (owner is null)
        {
            owner = new User { Id = Guid.NewGuid(), WalletAddress = wallet, CreatedAt = DateTimeOffset.UtcNow };
            db.Users.Add(owner);
        }

        if (ticket.OwnerId == owner.Id) return;
        ticket.OwnerId = owner.Id;
        await RecordAsync(db, evt.Id, "Transfer", ctx, $"Ticket #{tokenId} now owned by {Short(e.To)}", ct);
    }

    public async Task ApplyRedeemedAsync(TixFlowDbContext db, RedeemedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, ctx.Contract, ct);
        if (evt is null) return;

        var tokenId = (long)e.TokenId;
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketTier.EventId == evt.Id && t.TokenId == tokenId, ct);
        if (ticket is not null)
            ticket.BadgeClaimedAt ??= DateTimeOffset.UtcNow;

        await RecordAsync(db, evt.Id, "BadgeClaimed", ctx, $"Ticket #{tokenId} became a soulbound attendance badge", ct);
    }

    public async Task ApplyCheckpointedAsync(
        TixFlowDbContext db, RedemptionsCheckpointedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, ctx.Contract, ct);
        if (evt is null) return;

        var root = "0x" + Convert.ToHexString(e.Root).ToLowerInvariant();
        var checkpoints = await db.RedemptionCheckpoints
            .Where(c => c.EventId == evt.Id && c.Root == root && c.ConfirmedAt == null)
            .ToListAsync(ct);
        foreach (var checkpoint in checkpoints)
            checkpoint.ConfirmedAt = DateTimeOffset.UtcNow;

        await RecordAsync(db, evt.Id, "RedemptionsCheckpointed", ctx,
            $"Merkle root of {e.Count} door redemptions published", ct);
    }

    public async Task ApplyPaymentDepositedAsync(
        TixFlowDbContext db, PaymentDepositedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, e.Ticket.ToLowerInvariant(), ct);
        if (evt is null) return;
        await RecordAsync(db, evt.Id, "PaymentDeposited", ctx,
            $"{Usdc(e.Amount)} USDC from {Short(e.Buyer)} locked in escrow", ct);
    }

    public async Task ApplyEscrowReleasedAsync(
        TixFlowDbContext db, EscrowReleasedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, e.Ticket.ToLowerInvariant(), ct);
        if (evt is null) return;
        evt.EscrowReleasedAt ??= DateTimeOffset.UtcNow;
        await RecordAsync(db, evt.Id, "EscrowReleased", ctx,
            $"{Usdc(e.Amount)} USDC released from escrow to the organizer", ct);
    }

    public async Task ApplyEventCancelledAsync(
        TixFlowDbContext db, EventCancelledEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, e.Ticket.ToLowerInvariant(), ct);
        if (evt is null) return;
        evt.CancelledAt ??= DateTimeOffset.UtcNow;
        evt.CancelRequestedAt ??= evt.CancelledAt;
        evt.CancelTxHash ??= ctx.TxHash;
        await RecordAsync(db, evt.Id, "EventCancelled", ctx, "Event cancelled on-chain; every ticket is refundable", ct);
    }

    public async Task ApplyRefundedAsync(TixFlowDbContext db, RefundedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, e.Ticket.ToLowerInvariant(), ct);
        if (evt is null) return;

        var tokenId = (long)e.TokenId;
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketTier.EventId == evt.Id && t.TokenId == tokenId, ct);
        if (ticket is not null)
            ticket.RefundedAt ??= DateTimeOffset.UtcNow;

        await RecordAsync(db, evt.Id, "Refunded", ctx,
            $"{Usdc(e.Amount)} USDC refunded to {Short(e.Holder)} for ticket #{tokenId}", ct);
    }

    public async Task ApplyListedAsync(TixFlowDbContext db, ListedEventDTO e, LogContext ctx, CancellationToken ct)
    {
        var evt = await EventByContractAsync(db, e.Ticket.ToLowerInvariant(), ct);
        if (evt is null) return;
        await RecordAsync(db, evt.Id, "ResaleListed", ctx,
            $"Ticket #{e.TokenId} listed for resale at {Usdc(e.Price)} USDC", ct);
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

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static Task<Domain.Entities.Event?> EventByContractAsync(TixFlowDbContext db, string contract, CancellationToken ct) =>
        db.Events.FirstOrDefaultAsync(e => e.ContractAddress == contract, ct);

    private static async Task RecordAsync(
        TixFlowDbContext db, Guid? eventId, string kind, LogContext ctx, string summary, CancellationToken ct)
    {
        if (await db.ChainActivities.AnyAsync(a => a.TxHash == ctx.TxHash && a.LogIndex == ctx.LogIndex, ct))
            return;

        db.ChainActivities.Add(new ChainActivity
        {
            EventId = eventId,
            Kind = kind,
            ContractAddress = ctx.Contract,
            TxHash = ctx.TxHash,
            LogIndex = ctx.LogIndex,
            BlockNumber = ctx.BlockNumber,
            Summary = summary.Length <= 300 ? summary : summary[..300],
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static string Short(string address) =>
        address.Length < 12 ? address : $"{address[..6]}…{address[^4..]}";

    private static string Usdc(System.Numerics.BigInteger units) =>
        ((decimal)units / 1_000_000m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
