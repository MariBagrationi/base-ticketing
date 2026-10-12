using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using TixFlow.Domain.Entities;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

/// <summary>
/// Deploys an EventTicket clone through the factory for every event that does not have one yet.
/// The event's contract address is filled in later by the indexer, from the factory's EventCreated log.
/// </summary>
public class EventDeployer : PollingWorker
{
    private const string ZeroAddress = "0x0000000000000000000000000000000000000000";
    private static readonly TimeSpan ResubmitAfter = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRelayer _relayer;
    private readonly ChainOptions _options;
    private readonly ILogger<EventDeployer> _logger;

    public EventDeployer(
        IServiceScopeFactory scopeFactory,
        IRelayer relayer,
        IOptions<ChainOptions> options,
        ILogger<EventDeployer> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _relayer = relayer;
        _options = options.Value;
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(3);

    public override async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var staleBefore = DateTimeOffset.UtcNow - ResubmitAfter;

        var events = await db.Events
            .Where(e => e.ContractAddress == null && e.CancelledAt == null)
            .Where(e => e.DeployTxHash == null || e.DeploySubmittedAt < staleBefore)
            .Include(e => e.TicketTiers)
            .Include(e => e.Organizer)
            .OrderBy(e => e.StartsAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var evt in events)
        {
            // A previous attempt may have landed without the indexer having caught up yet.
            var existing = await _relayer.QueryAsync<TicketOfFunction, string>(
                _options.Contracts.TicketFactory, new TicketOfFunction { EventId = ChainIds.EventId(evt.Id) });
            if (!string.IsNullOrEmpty(existing) && !existing.IsTheSameAddress(ZeroAddress))
            {
                evt.DeploySubmittedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                continue;
            }

            var sent = await _relayer.SendAsync(
                _options.Contracts.TicketFactory, new CreateEventFunction { P = BuildParams(evt) }, ct);

            evt.DeployTxHash = sent.TxHash;
            evt.DeploySubmittedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Deploying ticket contract for event {EventId} ({Name}): tx {TxHash}",
                evt.Id, evt.Name, sent.TxHash);
        }
    }

    private CreateEventParams BuildParams(Event evt)
    {
        var endsAt = evt.EndsAt > evt.StartsAt ? evt.EndsAt : evt.StartsAt.AddHours(6);
        return new CreateEventParams
        {
            EventId = ChainIds.EventId(evt.Id),
            Name = evt.Name,
            Symbol = "TIX",
            Organizer = evt.Organizer.WalletAddress,
            EventStart = (ulong)evt.StartsAt.ToUnixTimeSeconds(),
            EventEnd = (ulong)endsAt.ToUnixTimeSeconds(),
            // Tickets stay locked to the price-capped marketplace until the event is over.
            UnlockTime = (ulong)endsAt.ToUnixTimeSeconds(),
            RoyaltyBps = _options.RoyaltyBps,
            Tiers = evt.TicketTiers
                .OrderBy(t => t.OnChainTierId)
                .Select(t => new TierInit
                {
                    TierId = t.OnChainTierId,
                    Name = t.Name,
                    Price = ChainIds.UsdcUnits(t.PriceUsdc),
                    SupplyCap = t.TotalSupply,
                    MaxPerWallet = t.MaxPerWallet
                })
                .ToList()
        };
    }
}
