using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TixFlow.Api.Auth;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Admin);

        // ----- users and roles ---------------------------------------------

        group.MapGet("/users", async (TixFlowDbContext db, RoleService roles, string? search) =>
        {
            var query = db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(u => u.WalletAddress.Contains(term));
            }

            var users = await query
                .OrderByDescending(u => u.Roles.Count)
                .ThenByDescending(u => u.CreatedAt)
                .Take(200)
                .Select(u => new
                {
                    u.Id,
                    Wallet = u.WalletAddress,
                    Roles = u.Roles.Select(r => r.Role.ToString()).ToList(),
                    StaffEventIds = u.StaffAssignments.Select(a => a.EventId).ToList(),
                    u.CreatedAt,
                    u.FirstLoginAt
                })
                .ToListAsync();

            return Results.Ok(users.Select(u => new
            {
                u.Id,
                u.Wallet,
                Roles = u.Roles.Order().ToList(),
                u.StaffEventIds,
                u.CreatedAt,
                HasSignedIn = u.FirstLoginAt is not null,
                ConfiguredAdmin = roles.IsBootstrapAdmin(u.Wallet)
            }));
        });

        group.MapPost("/users/{walletAddress}/roles", async (
            string walletAddress, [FromBody] RoleRequest request, ClaimsPrincipal user, RoleService roles) =>
        {
            if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var role))
                return Results.BadRequest(new { message = "Role must be User, Staff or Admin." });

            return ToResult(await roles.GrantAsync(user.UserId(), user.Wallet(), walletAddress, role));
        });

        group.MapDelete("/users/{walletAddress}/roles/{roleName}", async (
            string walletAddress, string roleName, ClaimsPrincipal user, RoleService roles) =>
        {
            if (!Enum.TryParse<Role>(roleName, ignoreCase: true, out var role))
                return Results.BadRequest(new { message = "Role must be User, Staff or Admin." });

            return ToResult(await roles.RevokeAsync(user.UserId(), user.Wallet(), walletAddress, role));
        });

        // ----- staff assignments -------------------------------------------

        group.MapGet("/events/{eventId:guid}/staff", async (Guid eventId, TixFlowDbContext db) =>
        {
            var staff = await db.StaffAssignments
                .Where(a => a.EventId == eventId)
                .Select(a => new { UserId = a.UserId, Wallet = a.User.WalletAddress, a.CreatedAt })
                .ToListAsync();
            return Results.Ok(staff);
        });

        group.MapPut("/events/{eventId:guid}/staff/{walletAddress}", async (
            Guid eventId, string walletAddress, ClaimsPrincipal user, TixFlowDbContext db, RoleService roles) =>
        {
            if (!RoleService.IsWallet(walletAddress))
                return Results.BadRequest(new { message = "That is not a valid wallet address." });
            if (!await db.Events.AnyAsync(e => e.Id == eventId))
                return Results.NotFound(new { message = "Event not found." });

            var wallet = walletAddress.ToLowerInvariant();
            var staff = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.WalletAddress == wallet);
            if (staff is null || staff.Roles.All(r => r.Role != Role.Staff))
                return Results.Conflict(new { message = "Grant this wallet the Staff role before assigning it to an event." });

            if (await db.StaffAssignments.AnyAsync(a => a.UserId == staff.Id && a.EventId == eventId))
                return Results.Ok(new { message = "Already assigned." });

            db.StaffAssignments.Add(new StaffAssignment { UserId = staff.Id, EventId = eventId, CreatedAt = DateTimeOffset.UtcNow });
            roles.Audit(user.UserId(), user.Wallet(), "staff.assign", wallet, eventId.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Assigned." });
        });

        group.MapDelete("/events/{eventId:guid}/staff/{walletAddress}", async (
            Guid eventId, string walletAddress, ClaimsPrincipal user, TixFlowDbContext db, RoleService roles) =>
        {
            var wallet = walletAddress.ToLowerInvariant();
            var assignment = await db.StaffAssignments
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.User.WalletAddress == wallet);
            if (assignment is null)
                return Results.NotFound(new { message = "That wallet is not assigned to this event." });

            db.StaffAssignments.Remove(assignment);
            roles.Audit(user.UserId(), user.Wallet(), "staff.unassign", wallet, eventId.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Unassigned." });
        });

        // ----- events ------------------------------------------------------

        // Requests cancellation. With a chain, the relayer calls EventEscrow.cancelEvent and the event is
        // marked cancelled once that log is indexed; refunds are then pushed to every holder. This request
        // itself never calls the chain.
        group.MapPost("/events/{eventId:guid}/cancel", async (
            Guid eventId, ClaimsPrincipal user, TixFlowDbContext db, RoleService roles, IOptions<ChainOptions> chain) =>
        {
            var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (evt is null)
                return Results.NotFound(new { message = "Event not found." });
            if (evt.CancelRequestedAt is not null || evt.CancelledAt is not null)
                return Results.Conflict(new { message = "This event is already cancelled." });
            if (evt.EscrowReleasedAt is not null)
                return Results.Conflict(new { message = "The escrow was already released to the organizer; this event can no longer be cancelled." });

            var now = DateTimeOffset.UtcNow;
            evt.CancelRequestedAt = now;
            var settlesOnChain = chain.Value.Enabled && evt.ContractAddress is not null;
            if (!settlesOnChain)
                evt.CancelledAt = now;

            roles.Audit(user.UserId(), user.Wallet(), "event.cancel", eventId.ToString(), evt.Name);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = settlesOnChain
                    ? "Cancellation submitted. Refunds are sent automatically once it confirms on-chain."
                    : "Event cancelled.",
                settlesOnChain
            });
        });

        // ----- audit log ---------------------------------------------------

        group.MapGet("/audit-log", async (TixFlowDbContext db, int? limit) =>
        {
            var entries = await db.AuditLogs
                .OrderByDescending(a => a.Id)
                .Take(Math.Clamp(limit ?? 100, 1, 500))
                .Select(a => new { a.Id, a.ActorWallet, a.Action, a.Target, a.Details, a.CreatedAt })
                .ToListAsync();
            return Results.Ok(entries);
        });
    }

    private static IResult ToResult(RoleChange change) =>
        Results.Json(new { message = change.Message }, statusCode: change.StatusCode);
}

public record RoleRequest(string Role);
