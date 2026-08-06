using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class AccountResourceLimit : BaseEntity
    {
        // Foreign Key
        public Guid AccountId { get; private set; }


        // Override values
        // Null means using default resource limit policy

        public int? AiTokenLimit { get; private set; }

        public int? AiPracticeScenarioLimit { get; private set; }

        public int? ExpertEvaluationRequestLimit { get; private set; }


        // Navigation property

        public virtual User Account { get; private set; }


        // EF Core constructor
        private AccountResourceLimit() { }


        // Domain constructor
        public AccountResourceLimit(
            Guid accountId,
            int? aiTokenLimit = null,
            int? aiPracticeScenarioLimit = null,
            int? expertEvaluationRequestLimit = null)
        {
            ValidateLimitValue(aiTokenLimit, nameof(aiTokenLimit));
            ValidateLimitValue(aiPracticeScenarioLimit, nameof(aiPracticeScenarioLimit));
            ValidateLimitValue(expertEvaluationRequestLimit, nameof(expertEvaluationRequestLimit));


            Id = Guid.NewGuid();

            AccountId = accountId;

            AiTokenLimit = aiTokenLimit;
            AiPracticeScenarioLimit = aiPracticeScenarioLimit;
            ExpertEvaluationRequestLimit = expertEvaluationRequestLimit;
        }


        // Domain behavior

        public void UpdateLimits(
            int? aiTokenLimit,
            int? aiPracticeScenarioLimit,
            int? expertEvaluationRequestLimit)
        {
            ValidateLimitValue(aiTokenLimit, nameof(aiTokenLimit));
            ValidateLimitValue(aiPracticeScenarioLimit, nameof(aiPracticeScenarioLimit));
            ValidateLimitValue(expertEvaluationRequestLimit, nameof(expertEvaluationRequestLimit));


            AiTokenLimit = aiTokenLimit;
            AiPracticeScenarioLimit = aiPracticeScenarioLimit;
            ExpertEvaluationRequestLimit = expertEvaluationRequestLimit;


            UpdateTimestamp();
        }


        private static void ValidateLimitValue(int? value, string propertyName)
        {
            if (value.HasValue && value.Value <= 0)
            {
                throw new ArgumentException(
                    "Giới hạn tài nguyên phải lớn hơn 0",
                    propertyName);
            }
        }
    }
}
