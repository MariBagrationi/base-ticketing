using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace TixFlow.Api.Queue;

public static class QueueEndpoints
{
    public static void MapQueueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/queue").WithTags("Queue").RequireAuthorization();

        group.MapPost("/{eventId:guid}/join", async (
            Guid eventId,
            ClaimsPrincipal user,
            IQueueStore queueService,
            IOptions<QueueOptions> options) =>
        {
            var userId = GetUserId(user);
            if (userId is null)
                return Results.Unauthorized();

            var (position, queueLength) = await queueService.JoinAsync(eventId, userId.Value);
            var opts = options.Value;
            var estimatedWaitSeconds = EstimateWait(position, opts.DefaultBatchSize, opts.DefaultIntervalSeconds);

            return Results.Ok(new JoinResponse(position, estimatedWaitSeconds, userId.Value));
        }).RequireRateLimiting("queue-join");

        group.MapPost("/{eventId:guid}/leave", async (
            Guid eventId,
            ClaimsPrincipal user,
            IQueueStore queueService) =>
        {
            var userId = GetUserId(user);
            if (userId is null)
                return Results.Unauthorized();

            var removed = await queueService.LeaveAsync(eventId, userId.Value);
            return removed
                ? Results.Ok(new { removed = true })
                : Results.Ok(new { removed = false, reason = "Not in queue or already admitted." });
        });

        group.MapGet("/{eventId:guid}/position", async (
            Guid eventId,
            ClaimsPrincipal user,
            IQueueStore queueService,
            IOptions<QueueOptions> options) =>
        {
            var userId = GetUserId(user);
            if (userId is null)
                return Results.Unauthorized();

            var position = await queueService.GetPositionAsync(eventId, userId.Value);
            if (position is null)
                return Results.NotFound(new { error = "Not in queue." });

            var opts = options.Value;
            var estimatedWaitSeconds = EstimateWait(position.Value, opts.DefaultBatchSize, opts.DefaultIntervalSeconds);

            return Results.Ok(new { position, estimatedWaitSeconds });
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
    }

    private static long EstimateWait(long position, int batchSize, int intervalSeconds)
    {
        if (batchSize <= 0) return 0;
        var batchesAhead = position / batchSize;
        return batchesAhead * intervalSeconds;
    }
}

public record JoinResponse(long Position, long EstimatedWaitSeconds, Guid QueueSessionId);
