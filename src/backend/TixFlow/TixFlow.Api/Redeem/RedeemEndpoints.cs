using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nethereum.Signer;
using TixFlow.Api.Auth;
using TixFlow.Api.Queue;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Redeem;

public static class RedeemEndpoints
{
    private const int ChallengeTtlSeconds = 60;
    private const string NotAssigned = "You are not assigned to scan tickets for this event.";

    // Door staff only. Responses carry ticket validity, tier and event, and never who the holder is.
    public static void MapRedeemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/redeem").WithTags("Redeem").RequireAuthorization(Policies.Staff);

        // Events this staff member may scan, with check-in progress.
        group.MapGet("/events", async (ClaimsPrincipal user, TixFlowDbContext db) =>
        {
            var userId = user.UserId();
            var assigned = user.IsAdmin()
                ? []
                : await db.StaffAssignments.Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync();

            var query = db.Events.Where(e => e.CancelledAt == null);
            if (assigned.Count > 0)
                query = query.Where(e => assigned.Contains(e.Id));

            var events = await query
                .OrderBy(e => e.StartsAt)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.VenueName,
                    e.StartsAt,
                    Sold = e.TicketTiers.SelectMany(t => t.Tickets).Count(t => t.Status != TicketStatus.PendingMint),
                    CheckedIn = e.TicketTiers.SelectMany(t => t.Tickets).Count(t => t.Status == TicketStatus.Redeemed)
                })
                .ToListAsync();

            return Results.Ok(new { scopedToAssignments = assigned.Count > 0, events });
        });

        group.MapPost("/{ticketId:guid}/challenge", async (
            Guid ticketId,
            ClaimsPrincipal user,
            TixFlowDbContext db,
            IChallengeStore challenges) =>
        {
            var ticket = await db.Tickets
                .Where(t => t.Id == ticketId)
                .Select(t => new
                {
                    t.Id,
                    t.Status,
                    t.TicketTier.EventId,
                    EventName = t.TicketTier.Event.Name,
                    TierName = t.TicketTier.Name,
                    Cancelled = t.TicketTier.Event.CancelledAt != null
                })
                .FirstOrDefaultAsync();

            if (ticket is null)
                return Results.NotFound(new { message = "Ticket not found." });

            if (!await db.CanScanAsync(user, ticket.EventId))
                return Results.Json(new { message = NotAssigned }, statusCode: 403);

            if (ticket.Cancelled)
                return Results.Conflict(new { message = "This event was cancelled." });

            if (ticket.Status == TicketStatus.Redeemed)
                return Results.Conflict(new { message = "Ticket already redeemed." });

            if (ticket.Status == TicketStatus.PendingMint)
                return Results.UnprocessableEntity(new { message = "Ticket not yet minted." });

            var challenge = GenerateChallenge();
            await challenges.StoreAsync(ticketId, challenge, TimeSpan.FromSeconds(ChallengeTtlSeconds));

            return Results.Ok(new
            {
                ticketId,
                challenge,
                expiresInSeconds = ChallengeTtlSeconds,
                eventName = ticket.EventName,
                tierName = ticket.TierName
            });
        });

        group.MapPost("/{ticketId:guid}/confirm", async (
            Guid ticketId,
            [FromBody] ConfirmRedeemRequest request,
            ClaimsPrincipal user,
            TixFlowDbContext db,
            IChallengeStore challenges,
            IHubContext<QueueHub> hub) =>
        {
            if (string.IsNullOrWhiteSpace(request.Challenge) || string.IsNullOrWhiteSpace(request.Signature))
                return Results.BadRequest(new { message = "Challenge and signature are required." });

            var ticket = await db.Tickets
                .Include(t => t.Owner)
                .Include(t => t.TicketTier).ThenInclude(tier => tier.Event)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket is null)
                return Results.NotFound(new { message = "Ticket not found." });

            // Checked before the challenge is consumed, so an unassigned scanner cannot burn it.
            var eventId = ticket.TicketTier.EventId;
            if (!await db.CanScanAsync(user, eventId))
                return Results.Json(new { message = NotAssigned }, statusCode: 403);

            var storedChallenge = await challenges.ConsumeAsync(ticketId);

            if (storedChallenge is null)
                return Results.UnprocessableEntity(new { message = "Challenge expired or not found." });

            if (storedChallenge != request.Challenge)
                return Results.UnprocessableEntity(new { message = "Challenge mismatch." });

            if (ticket.Status == TicketStatus.Redeemed)
                return Results.Conflict(new { message = "Ticket already redeemed." });

            if (ticket.Status == TicketStatus.PendingMint)
                return Results.UnprocessableEntity(new { message = "Ticket not yet minted." });

            string recoveredAddress;
            try
            {
                var signer = new EthereumMessageSigner();
                recoveredAddress = signer.EncodeUTF8AndEcRecover(request.Challenge, request.Signature);
            }
            catch
            {
                return Results.BadRequest(new { message = "Invalid signature." });
            }

            if (!string.Equals(recoveredAddress, ticket.Owner.WalletAddress, StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { message = "Signature doesn't match the ticket owner's wallet." }, statusCode: 403);

            var now = DateTimeOffset.UtcNow;
            ticket.Status = TicketStatus.Redeemed;
            ticket.RedeemedAt = now;
            await db.SaveChangesAsync();

            var counts = await CheckinCountsAsync(db, eventId);
            await hub.Clients.Group(QueueHub.CheckinGroup(eventId.ToString()))
                .SendAsync("CheckinUpdate", new { eventId, counts.Sold, counts.CheckedIn });

            return Results.Ok(new
            {
                success = true,
                ticketId = ticket.Id,
                redeemedAt = now,
                eventName = ticket.TicketTier.Event.Name,
                tierName = ticket.TicketTier.Name,
                checkedIn = counts.CheckedIn,
                sold = counts.Sold
            });
        });

        // Check-in progress for one event, per tier.
        app.MapGet("/events/{eventId:guid}/checkins", async (Guid eventId, ClaimsPrincipal user, TixFlowDbContext db) =>
        {
            var evt = await db.Events
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    Tiers = e.TicketTiers.OrderBy(t => t.PriceUsdc).Select(t => new
                    {
                        t.Name,
                        Sold = t.Tickets.Count(k => k.Status != TicketStatus.PendingMint),
                        CheckedIn = t.Tickets.Count(k => k.Status == TicketStatus.Redeemed)
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (evt is null)
                return Results.NotFound(new { message = "Event not found." });
            if (!await db.CanScanAsync(user, eventId))
                return Results.Json(new { message = NotAssigned }, statusCode: 403);

            return Results.Ok(new
            {
                eventId = evt.Id,
                eventName = evt.Name,
                sold = evt.Tiers.Sum(t => t.Sold),
                checkedIn = evt.Tiers.Sum(t => t.CheckedIn),
                tiers = evt.Tiers
            });
        }).RequireAuthorization(Policies.Staff).WithTags("Redeem");
    }

    private static async Task<(int Sold, int CheckedIn)> CheckinCountsAsync(TixFlowDbContext db, Guid eventId)
    {
        var tickets = db.Tickets.Where(t => t.TicketTier.EventId == eventId);
        return (
            await tickets.CountAsync(t => t.Status != TicketStatus.PendingMint),
            await tickets.CountAsync(t => t.Status == TicketStatus.Redeemed));
    }

    private static string GenerateChallenge()
    {
        return $"tixflow-redeem-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
    }
}

public record ConfirmRedeemRequest(string Challenge, string Signature);
