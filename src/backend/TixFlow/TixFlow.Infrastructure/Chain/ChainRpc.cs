using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace TixFlow.Infrastructure.Chain;

/// <summary>Runs RPC calls against the primary endpoint and fails over to the fallback on transport errors.</summary>
public interface IChainRpc
{
    /// <summary>Address of the relayer account, or null when no relayer key is configured.</summary>
    string? RelayerAddress { get; }

    /// <summary>Executes a read or raw-transaction submission with endpoint failover.</summary>
    Task<T> ExecuteAsync<T>(Func<IWeb3, Task<T>> action);
}

public class ChainRpc : IChainRpc
{
    private readonly IWeb3[] _endpoints;
    private readonly string[] _urls;
    private readonly ILogger<ChainRpc> _logger;
    private int _active;

    public ChainRpc(IOptions<ChainOptions> options, ILogger<ChainRpc> logger)
    {
        _logger = logger;
        var opts = options.Value;
        ClientBase.ConnectionTimeout = TimeSpan.FromSeconds(opts.RpcTimeoutSeconds);

        var account = string.IsNullOrWhiteSpace(opts.RelayerPrivateKey)
            ? null
            : new Account(opts.RelayerPrivateKey, opts.ChainId);
        RelayerAddress = account?.Address;

        _urls = new[] { opts.RpcUrl, opts.FallbackRpcUrl }
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!)
            .Distinct()
            .ToArray();
        _endpoints = _urls.Select(url => account is null ? new Web3(url) : (IWeb3)new Web3(account, url)).ToArray();
    }

    public string? RelayerAddress { get; }

    public async Task<T> ExecuteAsync<T>(Func<IWeb3, Task<T>> action)
    {
        Exception? last = null;
        var start = Volatile.Read(ref _active);

        for (var attempt = 0; attempt < _endpoints.Length; attempt++)
        {
            var index = (start + attempt) % _endpoints.Length;
            try
            {
                var result = await action(_endpoints[index]);
                if (index != start)
                {
                    Volatile.Write(ref _active, index);
                    _logger.LogWarning("RPC failover: now using {Url}", _urls[index]);
                }
                return result;
            }
            catch (Exception ex) when (IsTransportFailure(ex))
            {
                last = ex;
                _logger.LogWarning("RPC endpoint {Url} failed: {Message}", _urls[index], ex.Message);
            }
        }

        throw last ?? new InvalidOperationException("No RPC endpoint is configured.");
    }

    /// <summary>
    /// True for connectivity problems worth retrying elsewhere. A JSON-RPC error response (for example a
    /// reverted call) means the node answered, so trying another endpoint would not help.
    /// </summary>
    public static bool IsTransportFailure(Exception ex) => ex switch
    {
        RpcResponseException => false,
        RpcClientTimeoutException or RpcClientUnknownException => true,
        HttpRequestException or SocketException or TaskCanceledException or TimeoutException => true,
        _ => ex.InnerException is not null && IsTransportFailure(ex.InnerException)
    };
}
