namespace TixFlow.Workers.Contract;

/// <summary>Mint worker tuning. RPC endpoints, keys and addresses live in the shared Chain section.</summary>
public class MintOptions
{
    public const string SectionName = "Mint";

    public int PollIntervalSeconds { get; set; } = 5;
    /// <summary>Maximum number of orders picked up per tick.</summary>
    public int MaxBatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 3;
}
