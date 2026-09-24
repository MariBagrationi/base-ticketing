using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using TixFlow.Api.Queue;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Tests;

public class TixFlowWebFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryQueueStore _queueStore = new();

    public InMemoryQueueStore QueueStore => _queueStore;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSigningKeyForTixFlowIntegrationTests123!",
                ["Jwt:Issuer"] = "TixFlow",
                ["Jwt:Audience"] = "TixFlow",
                ["Jwt:ExpiryHours"] = "1",
                ["Queue:DefaultBatchSize"] = "3",
                ["Queue:DefaultIntervalSeconds"] = "1",
                ["Queue:AdmissionTokenTtlMinutes"] = "5",
                ["Queue:PositionBroadcastIntervalSeconds"] = "5",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace EF with InMemory
            var dbDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         || d.ServiceType == typeof(DbContextOptions<TixFlowDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in dbDescriptors)
                services.Remove(d);

            var dbName = $"TixFlowTests_{Guid.NewGuid()}";
            services.AddDbContext<TixFlowDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Replace Redis IConnectionMultiplexer — remove the real one
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor is not null)
                services.Remove(redisDescriptor);

            // Replace IQueueStore with in-memory implementation
            var queueDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IQueueStore));
            if (queueDescriptor is not null)
                services.Remove(queueDescriptor);
            services.AddSingleton<IQueueStore>(_queueStore);

            // Remove AdmissionWorker so it doesn't run during tests
            var workerDescriptors = services
                .Where(d => d.ImplementationType == typeof(AdmissionWorker)
                         || (d.ServiceType == typeof(IHostedService)
                             && d.ImplementationType == typeof(AdmissionWorker)))
                .ToList();
            foreach (var d in workerDescriptors)
                services.Remove(d);
        });
    }
}
