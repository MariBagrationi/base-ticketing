using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/health").WithTags("Health");

        // Liveness for container orchestration: the process is up and can reach its database.
        group.MapGet("/", async (TixFlowDbContext db) =>
            await db.Database.CanConnectAsync()
                ? Results.Ok(new { status = "Healthy" })
                : Results.Json(new { status = "Unhealthy" }, statusCode: 503));

        // Chain status comes from the indexer's heartbeat in the database, never from a live RPC call,
        // so this endpoint stays fast and correct even while the chain is unreachable.
        group.MapGet("/chain", async (IConfiguration config, IOptions<ChainOptions> options, TixFlowDbContext db) =>
        {
            var chain = options.Value;
            if (!chain.Enabled)
            {
                return config.GetValue<bool>("Demo:SimulateMint")
                    ? Results.Ok(new
                    {
                        status = "Simulated",
                        message = "Demo mode: payments and NFT minting are simulated locally."
                    })
                    : Results.Ok(new
                    {
                        status = "Degraded",
                        message = "No chain configured. Purchases are saved and NFTs will mint once one is connected."
                    });
            }

            var checkpoint = await db.IndexerCheckpoints.AsNoTracking().FirstOrDefaultAsync();
            var lastSeen = checkpoint?.LastRpcSuccessAt;
            var healthy = lastSeen is not null
                && DateTimeOffset.UtcNow - lastSeen.Value < TimeSpan.FromSeconds(chain.DegradedAfterSeconds);
            var pendingMints = await db.MintJobs.CountAsync(j =>
                j.Status == Domain.Enums.MintJobStatus.Pending || j.Status == Domain.Enums.MintJobStatus.Submitted);

            return Results.Ok(new
            {
                status = healthy ? "Healthy" : "Degraded",
                chainId = chain.ChainId,
                headBlock = checkpoint?.ChainHeadBlock,
                indexedBlock = checkpoint?.LastProcessedBlock,
                lastSeenAt = lastSeen,
                pendingMints,
                message = healthy
                    ? "Connected to the chain."
                    : "The chain is unreachable. Checkout still works: purchases are saved and will be minted automatically when the connection returns."
            });
        });
    }
}
