using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Auth;

public record RoleChange(bool Ok, int StatusCode, string Message)
{
    public static RoleChange Success(string message) => new(true, 200, message);
    public static RoleChange Fail(int status, string message) => new(false, status, message);
}

/// <summary>
/// The only place roles are granted or revoked. Every change is written to the audit log in the same
/// database transaction as the change itself.
/// </summary>
public partial class RoleService
{
    private readonly TixFlowDbContext _db;
    private readonly HashSet<string> _adminWallets;

    public RoleService(TixFlowDbContext db, IConfiguration config)
    {
        _db = db;
        _adminWallets = ParseWallets(config["Auth:AdminWallets"]);
    }

    public static HashSet<string> ParseWallets(string? value) =>
        (value ?? string.Empty)
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsWallet)
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

    public static bool IsWallet(string? value) => value is not null && WalletRegex().IsMatch(value);

    /// <summary>Wallets listed in Auth:AdminWallets are admins by configuration; the app cannot demote them.</summary>
    public bool IsBootstrapAdmin(string wallet) => _adminWallets.Contains(wallet.ToLowerInvariant());

    /// <summary>
    /// Applies the roles a login is entitled to: User for a brand-new account, Admin for configured wallets.
    /// This is the only automatic escalation path, and it is driven by server configuration.
    /// </summary>
    public async Task<List<Role>> EnsureLoginRolesAsync(User user, bool isNewUser)
    {
        var roles = await _db.UserRoles.Where(r => r.UserId == user.Id).Select(r => r.Role).ToListAsync();

        if (isNewUser && !roles.Contains(Role.User))
        {
            Add(user.Id, Role.User);
            roles.Add(Role.User);
        }

        if (IsBootstrapAdmin(user.WalletAddress) && !roles.Contains(Role.Admin))
        {
            Add(user.Id, Role.Admin);
            roles.Add(Role.Admin);
            Audit(null, "system", "role.bootstrap", user.WalletAddress, "Admin granted from Auth:AdminWallets");
        }

        await _db.SaveChangesAsync();
        return roles;
    }

    public async Task<RoleChange> GrantAsync(Guid actorId, string actorWallet, string targetWallet, Role role)
    {
        if (!IsWallet(targetWallet))
            return RoleChange.Fail(400, "That is not a valid wallet address.");

        var wallet = targetWallet.ToLowerInvariant();
        var target = await _db.Users.FirstOrDefaultAsync(u => u.WalletAddress == wallet);
        if (target is null)
        {
            // Roles can be prepared for a wallet before its first sign-in, e.g. for door staff.
            target = new User { Id = Guid.NewGuid(), WalletAddress = wallet, CreatedAt = DateTimeOffset.UtcNow };
            _db.Users.Add(target);
        }
        else if (await _db.UserRoles.AnyAsync(r => r.UserId == target.Id && r.Role == role))
        {
            return RoleChange.Fail(409, $"That wallet already has the {role} role.");
        }

        Add(target.Id, role);
        Audit(actorId, actorWallet, "role.grant", wallet, role.ToString());
        await _db.SaveChangesAsync();
        return RoleChange.Success($"{role} granted.");
    }

    public async Task<RoleChange> RevokeAsync(Guid actorId, string actorWallet, string targetWallet, Role role)
    {
        if (!IsWallet(targetWallet))
            return RoleChange.Fail(400, "That is not a valid wallet address.");

        var wallet = targetWallet.ToLowerInvariant();
        if (role == Role.Admin && IsBootstrapAdmin(wallet))
            return RoleChange.Fail(400, "This admin is set in server configuration (Auth:AdminWallets) and cannot be demoted here.");

        var existing = await _db.UserRoles
            .FirstOrDefaultAsync(r => r.User.WalletAddress == wallet && r.Role == role);
        if (existing is null)
            return RoleChange.Fail(404, $"That wallet does not have the {role} role.");

        _db.UserRoles.Remove(existing);
        if (role == Role.Staff)
        {
            // Event assignments only mean something for staff.
            var assignments = await _db.StaffAssignments.Where(a => a.UserId == existing.UserId).ToListAsync();
            _db.StaffAssignments.RemoveRange(assignments);
        }

        Audit(actorId, actorWallet, "role.revoke", wallet, role.ToString());
        await _db.SaveChangesAsync();
        return RoleChange.Success($"{role} revoked.");
    }

    public void Audit(Guid? actorId, string actorWallet, string action, string target, string? details = null) =>
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ActorWallet = actorWallet,
            Action = action,
            Target = target,
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow
        });

    private void Add(Guid userId, Role role) =>
        _db.UserRoles.Add(new UserRole { UserId = userId, Role = role, GrantedAt = DateTimeOffset.UtcNow });

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$")]
    private static partial Regex WalletRegex();
}
