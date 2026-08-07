namespace AILA.Domain.Entities;

using AILA.Domain.Common;
using System;

public class UserTokenQuota : BaseEntity
{
    public Guid AccountId { get; private set; }
    public int DailyLimit { get; private set; }
    public int UsedAmountToday { get; private set; }
    public DateTime LastUsageDate { get; private set; }

    private UserTokenQuota() { }

    public UserTokenQuota(Guid accountId, int dailyLimit = 50000)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        DailyLimit = dailyLimit;
        UsedAmountToday = 0;
        LastUsageDate = DateTime.UtcNow.Date;
    }

    public void UpdateDailyLimit(int newLimit)
    {
        if (newLimit <= 0) throw new ArgumentException("Hạn mức phải lớn hơn 0.");
        DailyLimit = newLimit;
        UpdateTimestamp();
    }

    public bool CanConsume(int estimatedTokens, out string? warningMessage)
    {
        warningMessage = null;
        var today = DateTime.UtcNow.Date;

        if (LastUsageDate.Date < today)
        {
            UsedAmountToday = 0;
            LastUsageDate = today;
        }

        if (UsedAmountToday + estimatedTokens > DailyLimit)
        {
            warningMessage = $"Bạn đã sử dụng hết hạn mức Token luyện tập trong ngày ({UsedAmountToday:N0}/{DailyLimit:N0} Tokens). Vui lòng thử lại vào 00:00 ngày mai.";
            return false;
        }

        return true;
    }

    public void RecordUsage(int actualTokens)
    {
        var today = DateTime.UtcNow.Date;
        if (LastUsageDate.Date < today)
        {
            UsedAmountToday = 0;
            LastUsageDate = today;
        }

        UsedAmountToday += actualTokens;
        UpdateTimestamp();
    }
}
