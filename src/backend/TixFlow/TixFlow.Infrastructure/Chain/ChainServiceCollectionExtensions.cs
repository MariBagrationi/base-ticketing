using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TixFlow.Infrastructure.Chain;

public static class ChainServiceCollectionExtensions
{
    /// <summary>Registers chain configuration and the services both the API and the workers need.</summary>
    public static IServiceCollection AddTixFlowChain(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChainOptions>(configuration.GetSection(ChainOptions.SectionName));
        services.AddSingleton<IPostConfigureOptions<ChainOptions>, ChainOptionsPostConfigure>();
        services.AddSingleton<IChainRpc, ChainRpc>();
        services.AddSingleton<IVoucherSigner, VoucherSigner>();
        services.AddSingleton<ISmartWalletSignatureVerifier, SmartWalletSignatureVerifier>();
        return services;
    }
}
