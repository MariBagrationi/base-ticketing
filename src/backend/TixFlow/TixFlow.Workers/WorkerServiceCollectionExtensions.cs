using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TixFlow.Infrastructure.Chain;
using TixFlow.Workers.Contract;
using TixFlow.Workers.Indexer;

namespace TixFlow.Workers;

public static class WorkerServiceCollectionExtensions
{
    /// <summary>
    /// Registers every service that writes to the chain or indexes it. Call this in exactly ONE process:
    /// the relayer account has a single nonce sequence, and two writers would corrupt it.
    /// Does nothing when no chain is configured.
    /// </summary>
    public static IServiceCollection AddTixFlowChainWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        var chain = configuration.GetSection(ChainOptions.SectionName).Get<ChainOptions>() ?? new ChainOptions();
        if (!chain.Enabled)
            return services;

        services.Configure<MintOptions>(configuration.GetSection(MintOptions.SectionName));
        services.Configure<IndexerOptions>(configuration.GetSection(IndexerOptions.SectionName));

        services.AddSingleton<INonceService, NonceService>();
        services.AddSingleton<IRelayer, Relayer>();
        services.AddSingleton<IContractMintClient, ContractMintClient>();
        services.AddSingleton<IWeb3Provider, Web3Provider>();

        services.AddHostedService<ChainIndexer>();
        services.AddHostedService<EventDeployer>();
        services.AddHostedService<MintWorker>();
        services.AddHostedService<RedemptionCheckpointer>();
        services.AddHostedService<RefundRelayer>();
        if (chain.Faucet.Enabled)
            services.AddHostedService<DemoFaucet>();

        return services;
    }
}
