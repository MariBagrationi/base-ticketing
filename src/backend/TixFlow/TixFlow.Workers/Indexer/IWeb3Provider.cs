using Nethereum.Web3;

namespace TixFlow.Workers.Indexer;

public interface IWeb3Provider
{
    IWeb3 GetWeb3();
    Task<long> GetBlockNumberAsync();
    Task<string?> GetBlockHashAtAsync(long blockNumber);
}
