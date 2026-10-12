using Microsoft.Extensions.Logging;
using Nethereum.RPC.Eth.DTOs;
using TixFlow.Infrastructure.Chain;

namespace TixFlow.Workers.Contract;

/// <summary>
/// Hands out transaction nonces for the relayer account. Exactly one process may own that account:
/// two writers would hand out the same nonce and one of the transactions would be dropped.
/// </summary>
public class NonceService : INonceService
{
    private long _currentNonce;
    private bool _initialized;
    private readonly IChainRpc _rpc;
    private readonly ILogger<NonceService> _logger;

    public NonceService(IChainRpc rpc, ILogger<NonceService> logger)
    {
        _rpc = rpc;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var address = _rpc.RelayerAddress
            ?? throw new InvalidOperationException("Chain:RelayerPrivateKey is not configured.");

        var pendingNonce = await _rpc.ExecuteAsync(web3 =>
            web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(address, BlockParameter.CreatePending()));

        Interlocked.Exchange(ref _currentNonce, (long)pendingNonce.Value);
        _initialized = true;
        _logger.LogInformation("Nonce set to {Nonce} for relayer {Address}", _currentNonce, address);
    }

    public long GetNextNonce()
    {
        if (!_initialized)
            throw new InvalidOperationException("NonceService has not been initialized.");

        return Interlocked.Increment(ref _currentNonce) - 1;
    }
}
