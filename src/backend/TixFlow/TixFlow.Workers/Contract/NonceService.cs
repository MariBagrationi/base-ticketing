using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Web3;

namespace TixFlow.Workers.Contract;

public class NonceService : INonceService
{
    private long _currentNonce;
    private bool _initialized;
    private readonly string _rpcUrl;
    private readonly string _accountAddress;
    private readonly ILogger<NonceService> _logger;

    public NonceService(IOptions<MintOptions> options, ILogger<NonceService> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _rpcUrl = opts.RpcUrl;
        var account = new Nethereum.Web3.Accounts.Account(opts.PrivateKey);
        _accountAddress = account.Address;
    }

    public async Task InitializeAsync()
    {
        var web3 = new Web3(_rpcUrl);
        var pendingNonce = await web3.Eth.Transactions.GetTransactionCount
            .SendRequestAsync(_accountAddress, Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending());

        _currentNonce = (long)pendingNonce.Value;
        _initialized = true;
        _logger.LogInformation("Nonce initialized to {Nonce} for {Address}", _currentNonce, _accountAddress);
    }

    public long GetNextNonce()
    {
        if (!_initialized)
            throw new InvalidOperationException("NonceService has not been initialized.");

        return Interlocked.Increment(ref _currentNonce) - 1;
    }
}
