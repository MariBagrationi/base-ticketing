namespace TixFlow.Workers.Contract;

public interface IContractMintClient
{
    Task<string> MintAsync(string toAddress, long nonce);
    Task<string> MintBatchAsync(string[] toAddresses, long nonce);
}
