using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

/// <summary>
/// Turns confirmed orders into on-chain tickets. Orders carrying the buyer's USDC authorisation are relayed
/// through the escrow (payment and mint are atomic); orders paid off-chain or free are minted in batches.
/// </summary>
public class MintWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContractMintClient _mintClient;
    private readonly MintOptions _options;
    private readonly ILogger<MintWorker> _logger;

    public MintWorker(
        IServiceScopeFactory scopeFactory,
        IContractMintClient mintClient,
        IOptions<MintOptions> options,
        ILogger<MintWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _mintClient = mintClient;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        // Oldest orders first, and only for events whose ticket contract is already deployed.
        var orderIds = await db.MintJobs
            .Where(j => j.Status == MintJobStatus.Pending && j.Attempts < _options.MaxAttempts)
            .Where(j => j.Ticket.Order.Event.ContractAddress != null)
            .GroupBy(j => j.Ticket.OrderId)
            .Select(g => new { OrderId = g.Key, Oldest = g.Min(j => j.CreatedAt) })
            .OrderBy(x => x.Oldest)
            .Take(_options.MaxBatchSize)
            .Select(x => x.OrderId)
            .ToListAsync(ct);

        if (orderIds.Count == 0) return;

        var orders = await db.Orders
            .Where(o => orderIds.Contains(o.Id))
            .Include(o => o.Buyer)
            .Include(o => o.TicketTier)
            .Include(o => o.Event)
            .Include(o => o.Tickets).ThenInclude(t => t.MintJobs)
            .ToListAsync(ct);

        foreach (var order in orders.Where(o => o.HasPaymentAuthorization))
            await ProcessPaidOrderAsync(order, ct);

        foreach (var group in orders.Where(o => !o.HasPaymentAuthorization).GroupBy(o => o.Event.ContractAddress!))
            await ProcessBatchAsync(group.Key, group.ToList(), ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task ProcessPaidOrderAsync(Order order, CancellationToken ct)
    {
        var jobs = PendingJobs(order);
        var request = ToRequest(order);
        var contract = order.Event.ContractAddress!;

        try
        {
            if (await _mintClient.IsOrderMintedAsync(contract, request))
            {
                // The buyer already claimed this order on-chain with a voucher; the indexer will confirm it.
                MarkSubmitted(jobs, txHash: null, nonce: null);
                _logger.LogInformation("Order {OrderId} was already minted on-chain, skipping", order.Id);
                return;
            }

            var payment = new PaymentAuthorizationRequest(
                order.PaymentValidAfter ?? 0, order.PaymentValidBefore ?? 0, order.PaymentNonce!, order.PaymentSignature!);
            var sent = await _mintClient.PurchaseWithAuthorizationAsync(contract, request, payment, ct);

            MarkSubmitted(jobs, sent.TxHash, sent.Nonce);
            _logger.LogInformation("Relayed escrow purchase for order {OrderId}: tx {TxHash}", order.Id, sent.TxHash);
        }
        catch (Exception ex)
        {
            RecordFailure(jobs, ex, $"order {order.Id}");
        }
    }

    private async Task ProcessBatchAsync(string contract, List<Order> orders, CancellationToken ct)
    {
        var jobs = orders.SelectMany(PendingJobs).ToList();

        try
        {
            var sent = await _mintClient.MintOrdersAsync(contract, orders.Select(ToRequest).ToList(), ct);
            MarkSubmitted(jobs, sent.TxHash, sent.Nonce);
            _logger.LogInformation("Submitted batch mint of {Count} orders on {Contract}: tx {TxHash}",
                orders.Count, contract, sent.TxHash);
        }
        catch (Exception ex)
        {
            RecordFailure(jobs, ex, $"batch of {orders.Count} orders on {contract}");
        }
    }

    private static List<MintJob> PendingJobs(Order order) =>
        order.Tickets.SelectMany(t => t.MintJobs).Where(j => j.Status == MintJobStatus.Pending).ToList();

    private static MintOrderRequest ToRequest(Order order) =>
        new(order.Id, order.Buyer.WalletAddress, order.TicketTier.OnChainTierId, order.Quantity);

    private static void MarkSubmitted(List<MintJob> jobs, string? txHash, long? nonce)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            job.Status = MintJobStatus.Submitted;
            job.TxHash = txHash;
            job.Nonce = nonce;
            job.UpdatedAt = now;
        }
    }

    private void RecordFailure(List<MintJob> jobs, Exception ex, string what)
    {
        // An unreachable chain is not the order's fault: leave the jobs untouched and try again next tick.
        if (ChainRpc.IsTransportFailure(ex))
        {
            _logger.LogWarning("Chain unreachable while minting {What}; will retry: {Message}", what, ex.Message);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            job.Attempts++;
            job.LastError = Truncate(ex.Message, 500);
            job.UpdatedAt = now;
            if (job.Attempts >= _options.MaxAttempts)
                job.Status = MintJobStatus.Failed;
        }

        _logger.LogWarning(ex, "Mint attempt failed for {What}", what);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
