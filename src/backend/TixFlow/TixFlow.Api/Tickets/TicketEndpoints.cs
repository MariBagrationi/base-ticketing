using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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
                    OrderStatus = t.Order.Status.ToString(),
                    MintTxHash = t.MintJobs
                        .OrderByDescending(j => j.CreatedAt)
                        .Select(j => j.TxHash)
                        .FirstOrDefault(),
                    t.RedeemedAt,
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
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            var ticket = await db.Tickets
                .Where(t => t.Id == ticketId && t.OwnerId == userId)
                .Select(t => new
                {
                    t.Id,
                    Status = t.Status.ToString(),
                    t.TokenId,
                    t.OrderId,
                    t.TicketTierId,
                    t.CreatedAt
                })
                .FirstOrDefaultAsync();

            return ticket is null
                ? Results.NotFound(new { message = "Ticket not found." })
                : Results.Ok(ticket);
        });
    }
}
