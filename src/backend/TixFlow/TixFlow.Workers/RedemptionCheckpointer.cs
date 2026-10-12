using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

/// <summary>
/// Publishes door redemptions on-chain as Merkle roots, then (optionally) relays the badge claim for each
/// holder. Door entry itself is decided off-chain and never waits for any of this.
/// </summary>
public class RedemptionCheckpointer : PollingWorker
{
    private const int MaxClaimsPerTick = 10;
    private static readonly TimeSpan ClaimCooldown = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRelayer _relayer;
    private readonly ChainOptions _options;
    private readonly ILogger<RedemptionCheckpointer> _logger;
    // Claims already relayed and waiting to be indexed, so the same claim is not sent every tick.
    private readonly Dictionary<Guid, DateTimeOffset> _claimSubmittedAt = [];
    private readonly Dictionary<Guid, DateTimeOffset> _checkpointFailedAt = [];
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromMinutes(10);

    public RedemptionCheckpointer(
        IServiceScopeFactory scopeFactory,
        IRelayer relayer,
        IOptions<ChainOptions> options,
        ILogger<RedemptionCheckpointer> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _relayer = relayer;
        _options = options.Value;
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(5);

    public override async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        await PublishCheckpointsAsync(db, ct);
        if (_options.AutoClaimBadges)
            await ClaimBadgesAsync(db, ct);
    }

    private async Task PublishCheckpointsAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var eventIds = await RedeemedOnChainTickets(db)
            .Where(t => t.CheckpointedAt == null)
            .Select(t => t.TicketTier.EventId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var eventId in eventIds)
        {
            if (_checkpointFailedAt.TryGetValue(eventId, out var failedAt)
                && DateTimeOffset.UtcNow - failedAt < FailureCooldown)
                continue;

            var evt = await db.Events.FirstAsync(e => e.Id == eventId, ct);
            if (evt.ContractAddress is null) continue;

            // Each root is cumulative: it covers every ticket redeemed for the event so far.
            var tickets = await RedeemedOnChainTickets(db)
                .Where(t => t.TicketTier.EventId == eventId)
                .ToListAsync(ct);
            var tree = new MerkleTree(tickets.Select(t => t.TokenId!.Value));

            SentTransaction sent;
            try
            {
                sent = await _relayer.SendAsync(evt.ContractAddress,
                    new CheckpointRedemptionsFunction { Root = tree.Root, Count = tree.Count }, ct);
            }
            catch (Exception ex) when (!ChainRpc.IsTransportFailure(ex))
            {
                // The contract refused the root, e.g. the database holds tickets that were never minted on
                // this chain. Back off for this event so the others keep being checkpointed.
                _checkpointFailedAt[eventId] = DateTimeOffset.UtcNow;
                _logger.LogWarning("Checkpoint for event {EventId} was rejected, retrying later: {Message}", eventId, ex.Message);
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var ticket in tickets.Where(t => t.CheckpointedAt is null))
                ticket.CheckpointedAt = now;

            db.RedemptionCheckpoints.Add(new RedemptionCheckpoint
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Root = tree.Root.ToHex(true),
                Count = tree.Count,
                TxHash = sent.TxHash,
                CreatedAt = now
            });
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Checkpointed {Count} redemptions for event {EventId}: root {Root}, tx {TxHash}",
                tree.Count, eventId, tree.Root.ToHex(true), sent.TxHash);
        }
    }

    private async Task ClaimBadgesAsync(TixFlowDbContext db, CancellationToken ct)
    {
        var pending = await db.Tickets
            .Where(t => t.CheckpointedAt != null && t.BadgeClaimedAt == null && t.TokenId != null)
            .Include(t => t.TicketTier).ThenInclude(tier => tier.Event)
            .OrderBy(t => t.CheckpointedAt)
            .Take(MaxClaimsPerTick)
            .ToListAsync(ct);

        foreach (var group in pending.GroupBy(t => t.TicketTier.EventId))
        {
            var evt = group.First().TicketTier.Event;
            if (evt.ContractAddress is null) continue;

            // Proofs only verify against the root that is live on-chain, so wait for the latest one to confirm.
            var latest = await db.RedemptionCheckpoints
                .Where(c => c.EventId == evt.Id)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (latest?.ConfirmedAt is null) continue;

            var checkpointed = await db.Tickets
                .Where(t => t.TicketTier.EventId == evt.Id && t.CheckpointedAt != null && t.TokenId != null)
                .Select(t => t.TokenId!.Value)
                .ToListAsync(ct);
            var tree = new MerkleTree(checkpointed);

            foreach (var ticket in group)
            {
                if (_claimSubmittedAt.TryGetValue(ticket.Id, out var submittedAt)
                    && DateTimeOffset.UtcNow - submittedAt < ClaimCooldown)
                    continue;

                try
                {
                    _claimSubmittedAt[ticket.Id] = DateTimeOffset.UtcNow;
                    await _relayer.SendAsync(evt.ContractAddress, new ClaimAttendanceBadgeFunction
                    {
                        TokenId = ticket.TokenId!.Value,
                        Proof = tree.GetProof(ticket.TokenId.Value)
                    }, ct);
                }
                catch (Exception ex) when (!ChainRpc.IsTransportFailure(ex))
                {
                    // For example the ticket is listed on the marketplace, or the holder already claimed it.
                    _logger.LogInformation("Badge claim skipped for token {TokenId}: {Message}", ticket.TokenId, ex.Message);
                }
            }
        }
    }

    private static IQueryable<Ticket> RedeemedOnChainTickets(TixFlowDbContext db) =>
        db.Tickets.Where(t => t.Status == TicketStatus.Redeemed && t.TokenId != null);
}
