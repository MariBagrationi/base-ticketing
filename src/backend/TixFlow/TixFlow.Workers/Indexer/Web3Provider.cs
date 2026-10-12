using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using TixFlow.Infrastructure.Chain;

namespace TixFlow.Workers.Indexer;

/// <summary>Read access to the chain for the indexer, with RPC failover.</summary>
public class Web3Provider : IWeb3Provider
{
    private readonly IChainRpc _rpc;
    private readonly ChainOptions _options;

    public Web3Provider(IChainRpc rpc, IOptions<ChainOptions> options)
    {
        _rpc = rpc;
        _options = options.Value;
    }

    public IWeb3 GetWeb3() => new Web3(_options.RpcUrl);

    public async Task<long> GetBlockNumberAsync() =>
        (long)(await _rpc.ExecuteAsync(web3 => web3.Eth.Blocks.GetBlockNumber.SendRequestAsync())).Value;

    public async Task<string?> GetBlockHashAtAsync(long blockNumber)
    {
        var block = await _rpc.ExecuteAsync(web3 =>
            web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber
                .SendRequestAsync(new BlockParameter((ulong)blockNumber)));
        return block?.BlockHash;
    }

    public async Task<FilterLog[]> GetLogsAsync(long fromBlock, long toBlock, IReadOnlyCollection<string> addresses)
    {
        if (addresses.Count == 0) return [];

        var filter = new NewFilterInput
        {
            FromBlock = new BlockParameter(new HexBigInteger(fromBlock)),
            ToBlock = new BlockParameter(new HexBigInteger(toBlock)),
            Address = addresses.ToArray()
        };
        return await _rpc.ExecuteAsync(web3 => web3.Eth.Filters.GetLogs.SendRequestAsync(filter));
    }
}
