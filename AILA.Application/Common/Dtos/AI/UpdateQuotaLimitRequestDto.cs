namespace AILA.Application.Common.Dtos.AI;

public class UpdateQuotaLimitRequestDto
{
    public int DailyLimit { get; set; }
    public int MonthlyLimit { get; set; }
}
