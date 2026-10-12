using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace TixFlow.Workers.Indexer;

public interface IWeb3Provider
{
    IWeb3 GetWeb3();
    Task<long> GetBlockNumberAsync();
    Task<string?> GetBlockHashAtAsync(long blockNumber);

    /// <summary>All logs emitted by <paramref name="addresses"/> in the inclusive block range.</summary>
    Task<FilterLog[]> GetLogsAsync(long fromBlock, long toBlock, IReadOnlyCollection<string> addresses) =>
        Task.FromResult(Array.Empty<FilterLog>());
}
