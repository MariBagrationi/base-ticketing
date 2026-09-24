namespace TixFlow.Api.Queue;

public class QueueOptions
{
    public const string SectionName = "Queue";

    public int DefaultBatchSize { get; set; } = 50;
    public int DefaultIntervalSeconds { get; set; } = 2;
    public int AdmissionTokenTtlMinutes { get; set; } = 5;
    public int PositionBroadcastIntervalSeconds { get; set; } = 5;
}
