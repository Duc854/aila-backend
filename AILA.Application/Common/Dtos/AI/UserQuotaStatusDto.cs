using System;

namespace AILA.Application.Common.Dtos.AI;

public class UserQuotaStatusDto
{
    public Guid AccountId { get; set; }
    public int DailyLimit { get; set; }
    public int MonthlyLimit { get; set; }
    public int UsedToday { get; set; }
    public int RemainingToday { get; set; }
    public int PercentageUsed { get; set; }
    public bool IsNearLimit { get; set; }
    public bool IsExceeded { get; set; }
    public string? StatusMessage { get; set; }
}
