using Microsoft.Extensions.Logging;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using TixFlow.Infrastructure.Chain;

namespace TixFlow.Workers.Contract;

public record SentTransaction(string TxHash, long Nonce);

/// <summary>The single writer for the relayer account. Every on-chain write in the system goes through here.</summary>
public interface IRelayer
{
    string Address { get; }

    Task EnsureReadyAsync();

    Task<SentTransaction> SendAsync<TFunction>(string contractAddress, TFunction function, CancellationToken ct = default)
        where TFunction : FunctionMessage, new();

    Task<TResult> QueryAsync<TFunction, TResult>(string contractAddress, TFunction function)
        where TFunction : FunctionMessage, new();
}

public class Relayer : IRelayer
{
    private readonly IChainRpc _rpc;
    private readonly INonceService _nonces;
    private readonly ILogger<Relayer> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _ready;

    public Relayer(IChainRpc rpc, INonceService nonces, ILogger<Relayer> logger)
    {
        _rpc = rpc;
        _nonces = nonces;
        _logger = logger;
    }

    public string Address =>
        _rpc.RelayerAddress ?? throw new InvalidOperationException("Chain:RelayerPrivateKey is not configured.");

    public async Task EnsureReadyAsync()
    {
        if (_ready) return;
        await _gate.WaitAsync();
        try
        {
            if (_ready) return;
            await _nonces.InitializeAsync();
            _ready = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SentTransaction> SendAsync<TFunction>(
        string contractAddress, TFunction function, CancellationToken ct = default)
        where TFunction : FunctionMessage, new()
    {
        await EnsureReadyAsync();
        await _gate.WaitAsync(ct);
        try
        {
            function.FromAddress = Address;

            // Simulate first. A call that would revert throws here, before a nonce is consumed.
            var gas = await _rpc.ExecuteAsync(web3 =>
                web3.Eth.GetContractTransactionHandler<TFunction>().EstimateGasAsync(contractAddress, function));
            function.Gas = new HexBigInteger(gas.Value * 12 / 10);

            var nonce = _nonces.GetNextNonce();
            function.Nonce = nonce;
            try
            {
                // Sign once and submit the raw bytes, so a failover retry can only resend the same transaction.
                var signed = await _rpc.ExecuteAsync(web3 =>
                    web3.Eth.GetContractTransactionHandler<TFunction>().SignTransactionAsync(contractAddress, function));
                var txHash = await _rpc.ExecuteAsync(web3 =>
                    web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signed));

                _logger.LogInformation("Sent {Function} to {Contract}: tx {TxHash}, nonce {Nonce}",
                    typeof(TFunction).Name, contractAddress, txHash, nonce);
                return new SentTransaction(txHash, nonce);
            }
            catch
            {
                // The nonce was handed out but may not have reached the chain; re-read it to avoid a gap.
                _ready = false;
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<TResult> QueryAsync<TFunction, TResult>(string contractAddress, TFunction function)
        where TFunction : FunctionMessage, new() =>
        _rpc.ExecuteAsync(web3 =>
            web3.Eth.GetContractQueryHandler<TFunction>().QueryAsync<TResult>(contractAddress, function));
}
