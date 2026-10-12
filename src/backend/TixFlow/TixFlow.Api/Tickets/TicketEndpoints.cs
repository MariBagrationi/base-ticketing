using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TixFlow.Api.Auth;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Tickets;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tickets").WithTags("Tickets").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            TixFlowDbContext db) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            var tickets = await db.Tickets
                .Where(t => t.OwnerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    Status = t.Status.ToString(),
                    t.TokenId,
                    t.OrderId,
                    t.TicketTierId,
                    TierName = t.TicketTier.Name,
                    t.TicketTier.PriceUsdc,
                    EventName = t.TicketTier.Event.Name,
                    EventId = t.TicketTier.Event.Id,
                    t.TicketTier.Event.VenueName,
                    t.TicketTier.Event.StartsAt,
                    // On-chain facts a holder can verify independently of this API.
                    t.TicketTier.Event.ContractAddress,
                    EventCancelledAt = t.TicketTier.Event.CancelledAt,
                    OrderStatus = t.Order.Status.ToString(),
                    PaidOnChain = t.Order.PaymentSignature != null,
                    MintTxHash = t.MintJobs
                        .OrderByDescending(j => j.CreatedAt)
                        .Select(j => j.TxHash)
                        .FirstOrDefault(),
                    MintStatus = t.MintJobs
                        .OrderByDescending(j => j.CreatedAt)
                        .Select(j => j.Status.ToString())
                        .FirstOrDefault(),
                    MintError = t.MintJobs
                        .Where(j => j.Status == Domain.Enums.MintJobStatus.Failed)
                        .OrderByDescending(j => j.CreatedAt)
                        .Select(j => j.LastError)
                        .FirstOrDefault(),
                    t.RedeemedAt,
                    t.CheckpointedAt,
                    t.BadgeClaimedAt,
                    t.RefundedAt,
                    t.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(tickets);
        });

        group.MapGet("/{ticketId:guid}", async (
            Guid ticketId,
            ClaimsPrincipal user,
            TixFlowDbContext db) =>
        {
            var ticket = await db.Tickets
                .Where(t => t.Id == ticketId)
                .Select(t => new
                {
                    t.Id,
                    t.OwnerId,
                    Status = t.Status.ToString(),
                    t.TokenId,
                    t.OrderId,
                    t.TicketTierId,
                    t.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (ticket is null)
                return Results.NotFound(new { message = "Ticket not found." });

            // A role is not enough: a ticket can only be read by its owner, or by an admin.
            if (ticket.OwnerId != user.UserId() && !user.IsAdmin())
                return Results.Json(new { message = "This ticket belongs to another wallet." }, statusCode: 403);

            return Results.Ok(new { ticket.Id, ticket.Status, ticket.TokenId, ticket.OrderId, ticket.TicketTierId, ticket.CreatedAt });
        });
    }
}
