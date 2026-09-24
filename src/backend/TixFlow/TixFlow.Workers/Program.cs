using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<TixFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.Configure<MintOptions>(builder.Configuration.GetSection(MintOptions.SectionName));
        builder.Services.AddSingleton<IContractMintClient, ContractMintClient>();
        builder.Services.AddSingleton<INonceService, NonceService>();
        builder.Services.AddHostedService<MintWorker>();

        var host = builder.Build();
        host.Run();
    }
}
