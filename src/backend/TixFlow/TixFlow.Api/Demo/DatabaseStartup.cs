using Microsoft.EntityFrameworkCore;
using TixFlow.Api.Auth;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Demo;

public static class DatabaseStartup
{
    /// <summary>
    /// Applies migrations and seeds demo data when configured. With <paramref name="force"/> it always does
    /// both; that is how the one-shot migrator container runs (`--migrate-and-seed`).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, bool force = false)
    {
        var config = app.Configuration;
        var migrate = force || config.GetValue<bool>("Database:MigrateOnStartup");
        var seed = force || config.GetValue<bool>("Demo:SeedEvents");
        if (!migrate && !seed)
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        if (migrate)
        {
            logger.LogInformation("Applying EF Core migrations");
            await db.Database.MigrateAsync();
        }

        if (!seed)
            return;

        var admins = RoleService.ParseWallets(config["Auth:AdminWallets"]);
        var staff = RoleService.ParseWallets(config["Demo:StaffWallets"]);
        foreach (var wallet in admins)
            await EnsureAccountAsync(db, wallet, Role.User, Role.Admin);
        // Demo door staff are staff only, so the demo shows that a checker cannot buy tickets.
        foreach (var wallet in staff.Except(admins))
            await EnsureAccountAsync(db, wallet, Role.Staff);
        await db.SaveChangesAsync();
        if (admins.Count + staff.Count > 0)
            logger.LogInformation("Seeded {Admins} admin and {Staff} staff accounts", admins.Count, staff.Count);

        // Seeded events belong to the first configured admin, so the demo admin sees them in the dashboard
        // and receives their escrow payouts.
        var organizer = config["Demo:OrganizerWallet"] ?? admins.FirstOrDefault();
        await DemoEventSeeder.SeedAsync(db, logger, organizer);
    }

    private static async Task EnsureAccountAsync(TixFlowDbContext db, string wallet, params Role[] roles)
    {
        var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.WalletAddress == wallet);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), WalletAddress = wallet, CreatedAt = DateTimeOffset.UtcNow };
            db.Users.Add(user);
        }

        foreach (var role in roles.Where(r => user.Roles.All(existing => existing.Role != r)))
            user.Roles.Add(new UserRole { UserId = user.Id, Role = role, GrantedAt = DateTimeOffset.UtcNow });
    }
}
