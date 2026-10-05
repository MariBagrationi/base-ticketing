using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TixFlow.Domain.Entities;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapGet("/", async (TixFlowDbContext db) =>
        {
            var events = await db.Events
                .Include(e => e.TicketTiers)
                .OrderByDescending(e => e.StartsAt)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.VenueName,
                    e.StartsAt,
                    e.OrganizerId,
                    Tiers = e.TicketTiers.OrderBy(t => t.PriceUsdc).Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.PriceUsdc,
                        t.TotalSupply,
                        TicketsSold = t.Tickets.Count
                    })
                })
                .ToListAsync();

            return Results.Ok(events);
        });

        group.MapGet("/{eventId:guid}", async (Guid eventId, TixFlowDbContext db) =>
        {
            var evt = await db.Events
                .Include(e => e.TicketTiers)
                    .ThenInclude(t => t.Tickets)
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.VenueName,
                    e.StartsAt,
                    e.OrganizerId,
                    Tiers = e.TicketTiers.OrderBy(t => t.PriceUsdc).Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.PriceUsdc,
                        t.TotalSupply,
                        TicketsSold = t.Tickets.Count,
                        RedeemedCount = t.Tickets.Count(tk => tk.Status == Domain.Enums.TicketStatus.Redeemed)
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

            var evt = new Event
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                VenueName = request.VenueName,
                StartsAt = request.StartsAt,
                OrganizerId = userId
            };

            db.Events.Add(evt);

            foreach (var tier in request.Tiers)
            {
                db.TicketTiers.Add(new TicketTier
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.Id,
                    Name = tier.Name,
                    PriceUsdc = tier.PriceUsdc,
                    TotalSupply = tier.TotalSupply
                });
            }

            await db.SaveChangesAsync();

            return Results.Created($"/events/{evt.Id}", new { evt.Id, evt.Name });
        }).RequireAuthorization();
    }
}

public record CreateEventRequest(
    string Name,
    string Description,
    string VenueName,
    DateTimeOffset StartsAt,
    List<CreateTierRequest> Tiers);

public record CreateTierRequest(string Name, decimal PriceUsdc, int TotalSupply);
