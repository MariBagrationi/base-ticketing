using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Tickets;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tickets").WithTags("Tickets").RequireAuthorization();

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
