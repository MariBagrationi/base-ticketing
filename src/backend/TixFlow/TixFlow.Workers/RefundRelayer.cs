using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

/// <summary>
/// Executes event cancellations requested by an admin, then pushes refunds to every ticket holder.
/// Holders never depend on this: the escrow lets anyone call claimRefund for a cancelled event.
/// </summary>
public class RefundRelayer : PollingWorker
{
    private const int RefundBatchSize = 50;
    private static readonly TimeSpan RefundCooldown = TimeSpan.FromSeconds(45);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRelayer _relayer;
    private readonly ChainOptions _options;
    private readonly ILogger<RefundRelayer> _logger;
    private readonly Dictionary<Guid, DateTimeOffset> _refundSubmittedAt = [];

    public RefundRelayer(
        IServiceScopeFactory scopeFactory,
        IRelayer relayer,
        IOptions<ChainOptions> options,
        ILogger<RefundRelayer> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _relayer = relayer;
        _options = options.Value;
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(4);

    public override async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        await SubmitCancellationsAsync(db, ct);
        await PushRefundsAsync(db, ct);
    }

    private async Task SubmitCancellationsAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var requested = await db.Events
            .Where(e => e.CancelRequestedAt != null && e.CancelledAt == null && e.CancelTxHash == null)
            .ToListAsync(ct);

        foreach (var evt in requested)
        {
            if (evt.ContractAddress is null)
            {
                // Never deployed, so there is nothing on-chain to cancel or refund.
                evt.CancelledAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var sent = await _relayer.SendAsync(
                    _options.Contracts.EventEscrow, new CancelEventFunction { Ticket = evt.ContractAddress }, ct);
                evt.CancelTxHash = sent.TxHash;
                _logger.LogInformation("Cancelling event {EventId} on-chain: tx {TxHash}", evt.Id, sent.TxHash);
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PushRefundsAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var cancelled = await db.Events
            .Where(e => e.CancelledAt != null && e.ContractAddress != null)
            .Where(e => e.TicketTiers.Any(tier => tier.Tickets.Any(
                t => t.TokenId != null && t.RefundedAt == null && t.Order.PaymentSignature != null)))
            .ToListAsync(ct);

        foreach (var evt in cancelled)
        {
            if (_refundSubmittedAt.TryGetValue(evt.Id, out var submittedAt)
                && DateTimeOffset.UtcNow - submittedAt < RefundCooldown)
                continue;

            // Only tickets paid through the escrow have anything to refund on-chain.
            var tokenIds = await db.Tickets
                .Where(t => t.TicketTier.EventId == evt.Id && t.TokenId != null && t.RefundedAt == null)
                .Where(t => t.Order.PaymentSignature != null)
                .OrderBy(t => t.TokenId)
                .Select(t => t.TokenId!.Value)
                .Take(RefundBatchSize)
                .ToListAsync(ct);
            if (tokenIds.Count == 0) continue;

            var sent = await _relayer.SendAsync(_options.Contracts.EventEscrow, new RefundBatchFunction
            {
                Ticket = evt.ContractAddress!,
                TokenIds = tokenIds.Select(id => new BigInteger(id)).ToList()
            }, ct);

            _refundSubmittedAt[evt.Id] = DateTimeOffset.UtcNow;
            _logger.LogInformation("Relayed {Count} refunds for cancelled event {EventId}: tx {TxHash}",
                tokenIds.Count, evt.Id, sent.TxHash);
        }
    }
}
