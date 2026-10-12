using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Auth;

/// <summary>Authorization policy names. Admin satisfies every policy.</summary>
public static class Policies
{
    /// <summary>Buyers: queue, checkout, own tickets.</summary>
    public const string User = "RequireUser";
    /// <summary>Venue staff: scanning and check-in counts.</summary>
    public const string Staff = "RequireStaff";
    /// <summary>Platform operators: events, roles, cancellations.</summary>
    public const string Admin = "RequireAdmin";

    public const string RoleClaim = "role";

    public static void AddTixFlowPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(User, p => p.RequireAssertion(context =>
            context.User.IsInRole(nameof(Role.User)) && !context.User.IsInRole(nameof(Role.Admin))));
        options.AddPolicy(Staff, p => p.RequireRole(nameof(Role.Staff)));
        options.AddPolicy(Admin, p => p.RequireRole(nameof(Role.Admin)));
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);

    public static string Wallet(this ClaimsPrincipal user) => user.FindFirstValue("wallet") ?? string.Empty;

    public static bool Has(this ClaimsPrincipal user, Role role) => user.IsInRole(role.ToString());

    public static bool IsAdmin(this ClaimsPrincipal user) => user.Has(Role.Admin);

    public static string[] Roles(this ClaimsPrincipal user) =>
        user.FindAll(Policies.RoleClaim).Select(c => c.Value).Order().ToArray();
}

public static class StaffScope
{
    /// <summary>
    /// Admins may scan any event. Staff with no assignments may scan any event; staff with assignments
    /// only the events they are assigned to.
    /// </summary>
    public static async Task<bool> CanScanAsync(this TixFlowDbContext db, ClaimsPrincipal user, Guid eventId)
    {
        if (!user.Has(Role.Staff)) return false;
        return await db.Events.AnyAsync(e => e.Id == eventId && e.CancelledAt == null);
    }
}
