using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.MockUSDC.ContractDefinition;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers;

/// <summary>
/// LOCAL CHAINS ONLY. Gives every new wallet mock USDC to spend and a little ETH for optional self-claims,
/// so the demo needs no manual funding. Relies on MockUSDC's open mint and Anvil's balance cheat.
/// </summary>
public class DemoFaucet : PollingWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRelayer _relayer;
    private readonly IChainRpc _rpc;
    private readonly ChainOptions _options;
    private readonly ILogger<DemoFaucet> _logger;

    public DemoFaucet(
        IServiceScopeFactory scopeFactory,
        IRelayer relayer,
        IChainRpc rpc,
        IOptions<ChainOptions> options,
        ILogger<DemoFaucet> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _relayer = relayer;
        _rpc = rpc;
        _options = options.Value;
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(2);

    public override async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var users = await db.Users
            .Where(u => u.FaucetFundedAt == null)
            .OrderBy(u => u.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            await _relayer.SendAsync(_options.Contracts.Usdc, new MintFunction
            {
                To = user.WalletAddress,
                Amount = ChainIds.UsdcUnits(_options.Faucet.UsdcAmount)
            }, ct);

            await TrySetEthBalanceAsync(user.WalletAddress);

            user.FaucetFundedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Faucet: funded {Wallet} with {Amount} mock USDC", user.WalletAddress, _options.Faucet.UsdcAmount);
        }
    }

    private async Task TrySetEthBalanceAsync(string wallet)
    {
        try
        {
            var wei = new BigInteger(_options.Faucet.EthAmount * 1_000_000_000m) * BigInteger.Pow(10, 9);
            var current = await _rpc.ExecuteAsync(web3 => web3.Eth.GetBalance.SendRequestAsync(wallet));
            if (current.Value >= wei) return;

            await _rpc.ExecuteAsync(web3 =>
                web3.Client.SendRequestAsync<object>("anvil_setBalance", null, wallet, new HexBigInteger(wei).HexValue));
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not set ETH balance for {Wallet}: {Message}", wallet, ex.Message);
        }
    }
}
