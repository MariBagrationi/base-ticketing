using System.Reflection;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace TixFlow.Workers.Contract;

public class ContractMintClient : IContractMintClient
{
    private readonly Nethereum.Contracts.Contract _contract;
    private readonly Account _account;

    public ContractMintClient(IOptions<MintOptions> options)
    {
        var opts = options.Value;
        _account = new Account(opts.PrivateKey);
        var web3 = new Web3(_account, opts.RpcUrl);

        var abi = LoadEmbeddedAbi();
        _contract = web3.Eth.GetContract(abi, opts.ContractAddress);
    }

    public async Task<string> MintAsync(string toAddress, long nonce)
    {
        var function = _contract.GetFunction("mint");
        var input = function.CreateTransactionInput(_account.Address, new HexBigInteger(0), null, toAddress);
        input.Nonce = new HexBigInteger(nonce);

        var txHash = await _contract.Eth.Transactions.SendRawTransaction
            .SendRequestAsync(await _account.TransactionManager.SignTransactionAsync(input));

        return txHash;
    }

    public async Task<string> MintBatchAsync(string[] toAddresses, long nonce)
    {
        var function = _contract.GetFunction("mintBatch");
        var input = function.CreateTransactionInput(_account.Address, new HexBigInteger(0), null, new object[] { toAddresses });
        input.Nonce = new HexBigInteger(nonce);

        var txHash = await _contract.Eth.Transactions.SendRawTransaction
            .SendRequestAsync(await _account.TransactionManager.SignTransactionAsync(input));

        return txHash;
    }

    private static string LoadEmbeddedAbi()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("EventTicketAbi.json"));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
