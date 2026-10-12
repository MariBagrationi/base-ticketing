using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TixFlow.Api.Auth;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Events;

public static class EventEndpoints
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(6);

    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapGet("/", async (TixFlowDbContext db) =>
        {
            var events = await db.Events
                .OrderByDescending(e => e.StartsAt)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.VenueName,
                    e.StartsAt,
                    e.EndsAt,
                    e.OrganizerId,
                    e.ContractAddress,
                    e.CancelledAt,
                    Tiers = e.TicketTiers.OrderBy(t => t.PriceUsdc).Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.PriceUsdc,
                        t.TotalSupply,
                        t.MaxPerWallet,
                        TicketsSold = t.Tickets.Count
                    })
                })
                .ToListAsync();

            return Results.Ok(events);
        });

        group.MapGet("/{eventId:guid}", async (Guid eventId, TixFlowDbContext db) =>
        {
            var evt = await db.Events
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.VenueName,
                    e.StartsAt,
                    e.EndsAt,
                    e.OrganizerId,
                    OrganizerWallet = e.Organizer.WalletAddress,
                    e.ContractAddress,
                    e.DeployTxHash,
                    e.CancelRequestedAt,
                    e.CancelTxHash,
                    e.CancelledAt,
                    e.EscrowReleasedAt,
                    Tiers = e.TicketTiers.OrderBy(t => t.PriceUsdc).Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.PriceUsdc,
                        t.TotalSupply,
                        t.MaxPerWallet,
                        t.OnChainTierId,
                        TicketsSold = t.Tickets.Count,
                        RedeemedCount = t.Tickets.Count(tk => tk.Status == TicketStatus.Redeemed)
                    })
                })
                .FirstOrDefaultAsync();

            return evt is null
                ? Results.NotFound(new { message = "Event not found." })
                : Results.Ok(evt);
        });

        group.MapPost("/", async (
            ClaimsPrincipal user,
            [FromBody] CreateEventRequest request,
            TixFlowDbContext db) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.VenueName))
                return Results.BadRequest(new { message = "Event name and venue are required." });
            if (request.Tiers is null || request.Tiers.Count == 0)
                return Results.BadRequest(new { message = "An event needs at least one ticket tier." });
            if (request.Tiers.Any(t => string.IsNullOrWhiteSpace(t.Name) || t.PriceUsdc < 0 || t.TotalSupply < 1))
                return Results.BadRequest(new { message = "Every tier needs a name, a non-negative price and a supply of at least 1." });
            if (request.Tiers.Any(t => t.MaxPerWallet is < 1))
                return Results.BadRequest(new { message = "The per-wallet limit must be at least 1." });

            var endsAt = request.EndsAt ?? request.StartsAt + DefaultDuration;
            if (endsAt <= request.StartsAt)
                return Results.BadRequest(new { message = "The event must end after it starts." });

            var evt = new Event
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                VenueName = request.VenueName.Trim(),
                StartsAt = request.StartsAt,
                EndsAt = endsAt,
                OrganizerId = userId
            };
            db.Events.Add(evt);

            // On-chain tier IDs are 1-based and assigned once; the ticket contract is configured with the same IDs.
            for (var i = 0; i < request.Tiers.Count; i++)
            {
                var tier = request.Tiers[i];
                db.TicketTiers.Add(new TicketTier
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.Id,
                    Name = tier.Name.Trim(),
                    PriceUsdc = tier.PriceUsdc,
                    TotalSupply = tier.TotalSupply,
                    OnChainTierId = i + 1,
                    MaxPerWallet = tier.MaxPerWallet ?? TicketTier.DefaultMaxPerWallet
                });
            }

            await db.SaveChangesAsync();

            return Results.Created($"/events/{evt.Id}", new { evt.Id, evt.Name });
        }).RequireAuthorization(Policies.Admin);
    }
}

public record CreateEventRequest(
    string Name,
    string? Description,
    string VenueName,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    List<CreateTierRequest> Tiers);

public record CreateTierRequest(string Name, decimal PriceUsdc, int TotalSupply, int? MaxPerWallet);
