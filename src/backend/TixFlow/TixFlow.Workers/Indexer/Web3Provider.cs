using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using TixFlow.Workers.Contract;

namespace TixFlow.Workers.Indexer;

public class Web3Provider : IWeb3Provider
{
    private readonly string _rpcUrl;

    public Web3Provider(IOptions<MintOptions> options)
    {
        _rpcUrl = options.Value.RpcUrl;
    }

    public IWeb3 GetWeb3() => new Web3(_rpcUrl);

    public async Task<long> GetBlockNumberAsync()
    {
        var web3 = new Web3(_rpcUrl);
        return (long)(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
    }

    public async Task<string?> GetBlockHashAtAsync(long blockNumber)
    {
        var web3 = new Web3(_rpcUrl);
        var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber
            .SendRequestAsync(new BlockParameter((ulong)blockNumber));
        return block?.BlockHash;
    }
}
