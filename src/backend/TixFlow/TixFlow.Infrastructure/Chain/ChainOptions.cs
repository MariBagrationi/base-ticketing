using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TixFlow.Infrastructure.Chain;

public class ChainOptions
{
    public const string SectionName = "Chain";

    /// <summary>When false the app runs without a chain and minting is simulated (see Demo:SimulateMint).</summary>
    public bool Enabled { get; set; }
    public long ChainId { get; set; } = 31337;
    public string RpcUrl { get; set; } = "http://localhost:8545";
    public string? FallbackRpcUrl { get; set; }
    /// <summary>RPC URL the browser should use for read-only verification. Defaults to <see cref="RpcUrl"/>.</summary>
    public string? PublicRpcUrl { get; set; }
    /// <summary>Block explorer base URL, e.g. https://sepolia.basescan.org. Empty on a local chain.</summary>
    public string? ExplorerUrl { get; set; }
    /// <summary>Optional path to the JSON written by the Foundry deploy script; fills any address left empty.</summary>
    public string? DeploymentsFile { get; set; }
    public ContractAddresses Contracts { get; set; } = new();

    /// <summary>Key of the single transaction-sending account (MINTER, REDEEMER, EVENT_CREATOR, CANCELLER).</summary>
    public string? RelayerPrivateKey { get; set; }
    /// <summary>Key that signs EIP-712 mint vouchers. Never sends transactions.</summary>
    public string? VoucherSignerPrivateKey { get; set; }

    public int VoucherLifetimeMinutes { get; set; } = 60;
    /// <summary>Reject confirmations of paid orders that carry no USDC authorisation. Off by default so
    /// load tests and off-chain-paid orders can use the minter path.</summary>
    public bool RequireOnChainPayment { get; set; }
    public int RpcTimeoutSeconds { get; set; } = 8;
    public int RoyaltyBps { get; set; } = 500;
    /// <summary>Relay claimAttendanceBadge for holders after each checkpoint so tickets become badges gaslessly.</summary>
    public bool AutoClaimBadges { get; set; } = true;
    /// <summary>Seconds without a successful RPC call after which the API reports the chain as degraded.</summary>
    public int DegradedAfterSeconds { get; set; } = 30;
    public FaucetOptions Faucet { get; set; } = new();
}

public class ContractAddresses
{
    public string TicketFactory { get; set; } = string.Empty;
    public string EventEscrow { get; set; } = string.Empty;
    public string PriceCappedResale { get; set; } = string.Empty;
    public string Usdc { get; set; } = string.Empty;
    public string SignatureValidator { get; set; } = string.Empty;
    public long DeployBlock { get; set; }
}

/// <summary>Local-chain convenience: tops up new wallets with mock USDC and a little gas money.</summary>
public class FaucetOptions
{
    public bool Enabled { get; set; }
    public decimal UsdcAmount { get; set; } = 1000m;
    public decimal EthAmount { get; set; } = 0.05m;
}

/// <summary>Fills contract addresses from the deployment file, so a container only needs the shared volume.</summary>
public class ChainOptionsPostConfigure : IPostConfigureOptions<ChainOptions>
{
    public void PostConfigure(string? name, ChainOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DeploymentsFile) || !File.Exists(options.DeploymentsFile))
            return;

        using var doc = JsonDocument.Parse(File.ReadAllText(options.DeploymentsFile));
        var root = doc.RootElement;
        var c = options.Contracts;

        c.TicketFactory = Pick(c.TicketFactory, root, "ticketFactory");
        c.EventEscrow = Pick(c.EventEscrow, root, "eventEscrow");
        c.PriceCappedResale = Pick(c.PriceCappedResale, root, "priceCappedResale");
        c.Usdc = Pick(c.Usdc, root, "usdc");
        c.SignatureValidator = Pick(c.SignatureValidator, root, "signatureValidator");

        if (c.DeployBlock == 0 && root.TryGetProperty("deployBlock", out var block))
            c.DeployBlock = block.GetInt64();
        if (root.TryGetProperty("chainId", out var chainId))
            options.ChainId = chainId.GetInt64();
    }

    private static string Pick(string configured, JsonElement root, string property) =>
        !string.IsNullOrWhiteSpace(configured)
            ? configured
            : root.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
