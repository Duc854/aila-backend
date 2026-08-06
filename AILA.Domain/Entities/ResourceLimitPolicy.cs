using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class ResourceLimitPolicy : BaseEntity
    {
        // Loại tài khoản áp dụng policy (Learner, Expert...)
        public ResourceAccountType AccountType { get; private set; }

        // Giới hạn tài nguyên mặc định
        public int AiTokenLimit { get; private set; }

        public int AiPracticeScenarioLimit { get; private set; }

        public int ExpertEvaluationRequestLimit { get; private set; }


        // Navigation properties

        private ResourceLimitPolicy() { }


        // Constructor tạo mới Default Resource Limit Policy
        public ResourceLimitPolicy(
            ResourceAccountType accountType,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationRequestLimit)
        {
            ValidateLimitValue(aiTokenLimit, nameof(aiTokenLimit));
            ValidateLimitValue(aiPracticeScenarioLimit, nameof(aiPracticeScenarioLimit));
            ValidateLimitValue(expertEvaluationRequestLimit, nameof(expertEvaluationRequestLimit));

            Id = Guid.NewGuid();

            AccountType = accountType;

            AiTokenLimit = aiTokenLimit;
            AiPracticeScenarioLimit = aiPracticeScenarioLimit;
            ExpertEvaluationRequestLimit = expertEvaluationRequestLimit;
        }


        // Domain behavior

        public void UpdateLimits(
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationRequestLimit)
        {
            ValidateLimitValue(aiTokenLimit, nameof(aiTokenLimit));
            ValidateLimitValue(aiPracticeScenarioLimit, nameof(aiPracticeScenarioLimit));
            ValidateLimitValue(expertEvaluationRequestLimit, nameof(expertEvaluationRequestLimit));

            AiTokenLimit = aiTokenLimit;
            AiPracticeScenarioLimit = aiPracticeScenarioLimit;

            ExpertEvaluationRequestLimit = expertEvaluationRequestLimit;

            UpdateTimestamp();
        }


        private static void ValidateLimitValue(int value, string propertyName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(
                    "Giới hạn tài nguyên phải lớn hơn 0",
                    propertyName);
            }
        }
    }
}
