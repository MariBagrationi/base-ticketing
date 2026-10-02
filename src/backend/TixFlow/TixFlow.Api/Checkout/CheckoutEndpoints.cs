using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TixFlow.Api.Queue;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Checkout;

public static class CheckoutEndpoints
{
    private const int MaxTicketsPerOrder = 4;

    public static void MapCheckoutEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/checkout").WithTags("Checkout").RequireAuthorization();

        group.MapPost("/reserve", async (
            ClaimsPrincipal user,
            HttpContext httpContext,
            [FromBody] ReserveRequest request,
            TixFlowDbContext db) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            if (request.Quantity is < 1 or > MaxTicketsPerOrder)
                return Results.BadRequest(new { message = $"You can buy between 1 and {MaxTicketsPerOrder} tickets per order." });

            if (httpContext.Items["AdmissionEventId"] is Guid admittedEventId && admittedEventId != request.EventId)
                return Results.Json(new { message = "Your queue admission is for a different event." }, statusCode: 403);

            var tier = await db.TicketTiers
                .Where(t => t.Id == request.TierId && t.EventId == request.EventId)
                .Select(t => new { t.TotalSupply, Sold = t.Tickets.Count })
                .FirstOrDefaultAsync();

            if (tier is null)
                return Results.NotFound(new { message = "That ticket tier doesn't exist for this event." });

            var remaining = tier.TotalSupply - tier.Sold;
            if (request.Quantity > remaining)
                return Results.Conflict(new
                {
                    message = remaining <= 0
                        ? "This tier is sold out."
                        : $"Only {remaining} ticket{(remaining == 1 ? "" : "s")} left in this tier."
                });
            var orderId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var order = new Order
            {
                Id = orderId,
                BuyerId = userId,
                EventId = request.EventId,
                TicketTierId = request.TierId,
                Quantity = request.Quantity,
                Status = OrderStatus.Pending,
                IdempotencyKey = Guid.NewGuid().ToString(),
                CreatedAt = now
            };

            db.Orders.Add(order);

            for (int i = 0; i < request.Quantity; i++)
            {
                db.Tickets.Add(new Ticket
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    TicketTierId = request.TierId,
                    OwnerId = userId,
                    Status = TicketStatus.PendingMint,
                    CreatedAt = now
                });
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Reservation accepted.",
                orderId,
                eventId = request.EventId,
                tierId = request.TierId,
                quantity = request.Quantity
            });
        }).AddEndpointFilter<AdmissionGateFilter>();

        group.MapPost("/confirm", async (
            ClaimsPrincipal user,
            [FromBody] ConfirmRequest request,
            TixFlowDbContext db) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            var order = await db.Orders
                .Include(o => o.Tickets)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order is null)
                return Results.NotFound(new { message = "Order not found." });

            if (order.BuyerId != userId)
                return Results.Json(new { message = "This order belongs to another wallet." }, statusCode: 403);

            if (order.Status != OrderStatus.Pending)
                return Results.Conflict(new { message = $"Order is already {order.Status}." });

            order.Status = OrderStatus.Confirmed;
            var now = DateTimeOffset.UtcNow;

            foreach (var ticket in order.Tickets)
            {
                db.MintJobs.Add(new MintJob
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    Status = MintJobStatus.Pending,
                    Attempts = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Order confirmed. Minting queued.",
                orderId = order.Id,
                mintJobsCreated = order.Tickets.Count
            });
        });
    }
}

public record ReserveRequest(Guid EventId, Guid TierId, int Quantity);
public record ConfirmRequest(Guid OrderId, string TxHash);
