namespace AILA.Application.Common.Dtos.Rag;

public class RagChatConfig
{
    public int TopK { get; set; } = 3;
    public double MinSimilarity { get; set; } = 0.60;
    public double Temperature { get; set; } = 0.3;
    public int MaxRetries { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 4;
    public int MaxHistoryMessages { get; set; } = 6;
    public string DefaultModelId { get; set; } = "llama-3.1-8b-instant";
    public int QuotaLimit { get; set; } = 1000;
    public float QuotaWarningThreshold { get; set; } = 0.80f;
}