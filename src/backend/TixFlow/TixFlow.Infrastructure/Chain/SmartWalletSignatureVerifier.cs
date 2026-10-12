using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using TixFlow.Infrastructure.Contracts.SignatureValidator.ContractDefinition;

namespace TixFlow.Infrastructure.Chain;

/// <summary>
/// Verifies signatures that cannot be checked with ecrecover: deployed smart wallets (ERC-1271) and
/// not-yet-deployed ones (ERC-6492), such as Coinbase Smart Wallet on Base.
/// </summary>
public interface ISmartWalletSignatureVerifier
{
    bool IsAvailable { get; }

    Task<bool> IsValidAsync(string signer, byte[] hash, byte[] signature);
}

/// <summary>Asks the on-chain SignatureValidator with a read-only eth_call. Nothing is ever sent or paid for.</summary>
public class SmartWalletSignatureVerifier : ISmartWalletSignatureVerifier
{
    private readonly IChainRpc _rpc;
    private readonly ChainOptions _options;
    private readonly ILogger<SmartWalletSignatureVerifier> _logger;

    public SmartWalletSignatureVerifier(
        IChainRpc rpc,
        IOptions<ChainOptions> options,
        ILogger<SmartWalletSignatureVerifier> logger)
    {
        _rpc = rpc;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsAvailable =>
        _options.Enabled && !string.IsNullOrWhiteSpace(_options.Contracts.SignatureValidator);

    public async Task<bool> IsValidAsync(string signer, byte[] hash, byte[] signature)
    {
        if (!IsAvailable) return false;

        try
        {
            var call = new IsValidSigFunction { Signer = signer, Hash = hash, Signature = signature };
            return await _rpc.ExecuteAsync(web3 =>
                web3.Eth.GetContractQueryHandler<IsValidSigFunction>()
                    .QueryAsync<bool>(_options.Contracts.SignatureValidator, call));
        }
        catch (Exception ex)
        {
            // An unreachable chain or a reverting wallet both mean "not proven": reject the login.
            _logger.LogWarning("Smart-wallet signature check failed for {Signer}: {Message}", signer, ex.Message);
            return false;
        }
    }
}
