using Microsoft.EntityFrameworkCore;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Demo;

public static class DatabaseStartup
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var migrate = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
        var seed = app.Configuration.GetValue<bool>("Demo:SeedEvents");
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

        if (seed)
            await DemoEventSeeder.SeedAsync(db, logger);
    }
}
