using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class AccountResourceUsage : BaseEntity
    {
        // Foreign Key

        public Guid AccountId { get; private set; }


        // Usage period

        public DateTime PeriodStart { get; private set; }

        public DateTime PeriodEnd { get; private set; }

        // Resource usage counters

        public int AiTokenUsed { get; private set; }

        public int AiPracticeScenarioUsed { get; private set; }

        public int ExpertEvaluationRequestUsed { get; private set; }


        // Navigation property

        public virtual User Account { get; private set; }


        // EF Core constructor

        private AccountResourceUsage() { }


        // Domain constructor

        public AccountResourceUsage(
            Guid accountId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            if (periodEnd <= periodStart)
            {
                throw new ArgumentException(
                    "Ngày bắt đầu không được sau ngày kết thúc");
            }


            Id = Guid.NewGuid();

            AccountId = accountId;

            PeriodStart = periodStart;
            PeriodEnd = periodEnd;


            AiTokenUsed = 0;
            AiPracticeScenarioUsed = 0;
            ExpertEvaluationRequestUsed = 0;
        }


        // Domain behaviors

        public void ConsumeAiToken(int amount)
        {
            ValidateUsageAmount(amount);

            AiTokenUsed += amount;

            UpdateTimestamp();
        }


        public void ConsumeAiPracticeScenario(int amount = 1)
        {
            ValidateUsageAmount(amount);

            AiPracticeScenarioUsed += amount;

            UpdateTimestamp();
        }


        public void ConsumeExpertEvaluationRequest(int amount = 1)
        {
            ValidateUsageAmount(amount);

            ExpertEvaluationRequestUsed += amount;

            UpdateTimestamp();
        }


        public bool IsExpired(DateTime currentDate)
        {
            return currentDate > PeriodEnd;
        }

        public void ClosePeriod(DateTime closeDate)
        {
            if (closeDate < PeriodStart)
                closeDate = PeriodStart;

            PeriodEnd = closeDate;
            UpdateTimestamp();
        }


        private static void ValidateUsageAmount(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException(
                    "Số lượng sử dụng phải lớn hơn 0");
            }
        }
    }
}
