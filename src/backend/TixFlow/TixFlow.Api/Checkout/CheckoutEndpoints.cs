using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TixFlow.Api.Queue;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Checkout;

public static partial class CheckoutEndpoints
{
    private const int MaxTicketsPerOrder = 4;

    // IMPORTANT: nothing in this file may call the chain. Checkout must keep working while the chain is
    // unreachable; on-chain settlement happens later, in the mint worker.
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
                .Select(t => new
                {
                    t.TotalSupply,
                    t.MaxPerWallet,
                    Cancelled = t.Event.CancelledAt != null || t.Event.CancelRequestedAt != null,
                    Sold = t.Tickets.Count,
                    OwnedByBuyer = t.Tickets.Count(tk => tk.OwnerId == userId)
                })
                .FirstOrDefaultAsync();

            if (tier is null)
                return Results.NotFound(new { message = "That ticket tier doesn't exist for this event." });

            if (tier.Cancelled)
                return Results.Conflict(new { message = "This event has been cancelled." });

            var remaining = tier.TotalSupply - tier.Sold;
            if (request.Quantity > remaining)
                return Results.Conflict(new
                {
                    message = remaining <= 0
                        ? "This tier is sold out."
                        : $"Only {remaining} ticket{(remaining == 1 ? "" : "s")} left in this tier."
                });

            // The ticket contract enforces the same limit, so going around this API gains nothing.
            if (tier.OwnedByBuyer + request.Quantity > tier.MaxPerWallet)
                return Results.Conflict(new
                {
                    message = tier.OwnedByBuyer >= tier.MaxPerWallet
                        ? $"This wallet already holds the maximum of {tier.MaxPerWallet} tickets for this tier."
                        : $"This tier allows {tier.MaxPerWallet} tickets per wallet and you already have {tier.OwnedByBuyer}."
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
            TixFlowDbContext db,
            IOptions<ChainOptions> chainOptions) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            var order = await db.Orders
                .Include(o => o.Tickets)
                .Include(o => o.TicketTier)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order is null)
                return Results.NotFound(new { message = "Order not found." });

            if (order.BuyerId != userId)
                return Results.Json(new { message = "This order belongs to another wallet." }, statusCode: 403);

            if (order.Status != OrderStatus.Pending)
                return Results.Conflict(new { message = $"Order is already {order.Status}." });

            var chain = chainOptions.Value;
            var needsPayment = chain.Enabled && order.TicketTier.PriceUsdc > 0;

            if (request.Payment is { } payment)
            {
                var problem = ValidatePayment(payment);
                if (problem is not null)
                    return Results.BadRequest(new { message = problem });

                // Stored as signed by the buyer. The mint worker relays it to the escrow contract, which is
                // the only party the signature lets move the buyer's USDC.
                order.PaymentValidAfter = payment.ValidAfter;
                order.PaymentValidBefore = payment.ValidBefore;
                order.PaymentNonce = payment.Nonce.ToLowerInvariant();
                order.PaymentSignature = payment.Signature.ToLowerInvariant();
            }
            else if (needsPayment && chain.RequireOnChainPayment)
            {
                return Results.BadRequest(new { message = "A signed USDC payment authorisation is required for this order." });
            }

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
                mintJobsCreated = order.Tickets.Count,
                paidOnChain = order.HasPaymentAuthorization
            });
        });
    }

    private static string? ValidatePayment(PaymentAuthorizationDto payment)
    {
        if (payment.Nonce is null || !Bytes32Regex().IsMatch(payment.Nonce))
            return "The payment nonce must be a 32-byte hex value.";
        if (payment.Signature is null || !SignatureRegex().IsMatch(payment.Signature))
            return "The payment signature must be a 65-byte hex value.";
        if (payment.ValidAfter < 0 || payment.ValidBefore <= DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
            return "The payment authorisation expires too soon.";
        return null;
    }

    [GeneratedRegex("^0x[0-9a-fA-F]{64}$")]
    private static partial Regex Bytes32Regex();

    [GeneratedRegex("^0x[0-9a-fA-F]{130}$")]
    private static partial Regex SignatureRegex();
}

public record ReserveRequest(Guid EventId, Guid TierId, int Quantity);

/// <summary>EIP-3009 receiveWithAuthorization parameters signed by the buyer for the order total.</summary>
public record PaymentAuthorizationDto(long ValidAfter, long ValidBefore, string Nonce, string Signature);

public record ConfirmRequest(Guid OrderId, string? TxHash, PaymentAuthorizationDto? Payment);
