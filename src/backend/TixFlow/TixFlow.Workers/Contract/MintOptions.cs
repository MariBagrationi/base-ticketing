namespace TixFlow.Workers.Contract;

public class MintOptions
{
    public const string SectionName = "Mint";

    public string RpcUrl { get; set; } = "https://mainnet.base.org";
    public string PrivateKey { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 3;
}
