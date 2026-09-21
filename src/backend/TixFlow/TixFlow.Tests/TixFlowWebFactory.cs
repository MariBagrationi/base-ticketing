using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Tests;

public class TixFlowWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSigningKeyForTixFlowIntegrationTests123!",
                ["Jwt:Issuer"] = "TixFlow",
                ["Jwt:Audience"] = "TixFlow",
                ["Jwt:ExpiryHours"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         || d.ServiceType == typeof(DbContextOptions<TixFlowDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in dbDescriptors)
                services.Remove(d);

            services.AddDbContext<TixFlowDbContext>(options =>
                options.UseInMemoryDatabase($"TixFlowTests_{Guid.NewGuid()}"));
        });
    }
}
