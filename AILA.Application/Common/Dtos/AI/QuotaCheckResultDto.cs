namespace AILA.Application.Common.Dtos.AI;

public class QuotaCheckResultDto
{
    public bool IsAllowed { get; set; } = true;
    public bool IsNearLimit { get; set; } = false;
    public int UsedAmount { get; set; }
    public int DailyLimit { get; set; }
    public int RemainingTokens { get; set; }
    public int PercentageUsed { get; set; }
    public string? WarningMessage { get; set; }
}
