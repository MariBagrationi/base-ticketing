namespace TixFlow.Workers.Indexer;

public class IndexerOptions
{
    public const string SectionName = "Indexer";

    public int PollIntervalSeconds { get; set; } = 6;
    public int ConfirmationBlocks { get; set; } = 2;
    public int ReorgRewindBlocks { get; set; } = 15;
    public long ContractDeployBlock { get; set; }
    public int StuckTxTimeoutMinutes { get; set; } = 5;
}
