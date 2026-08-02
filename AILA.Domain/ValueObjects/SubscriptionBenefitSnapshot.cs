using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.ValueObjects
{
    public sealed class SubscriptionPlanSnapshot
    {
        public int TierLevel { get; }

        public int DurationInDays { get; }

        public int AiTokenLimit { get; }

        public int AiPracticeScenarioLimit { get; }

        public int ExpertEvaluationLimit { get; }

        public SubscriptionPlanSnapshot(
            int tierLevel,
            int durationInDays,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit)
        {
            if (tierLevel <= 0)
                throw new ArgumentException(
                    "Cấp độ gói đăng ký phải lớn hơn 0.",
                    nameof(tierLevel));

            if (durationInDays <= 0)
                throw new ArgumentException(
                    "Thời hạn gói đăng ký phải lớn hơn 0 ngày.",
                    nameof(durationInDays));

            if (aiTokenLimit < 0)
                throw new ArgumentException(
                    "Giới hạn AI Token không hợp lệ.",
                    nameof(aiTokenLimit));

            if (aiPracticeScenarioLimit < 0)
                throw new ArgumentException(
                    "Giới hạn số lần AI Practice Scenario không hợp lệ.",
                    nameof(aiPracticeScenarioLimit));

            if (expertEvaluationLimit < 0)
                throw new ArgumentException(
                    "Giới hạn số lần đánh giá bởi chuyên gia không hợp lệ.",
                    nameof(expertEvaluationLimit));

            TierLevel = tierLevel;
            DurationInDays = durationInDays;
            AiTokenLimit = aiTokenLimit;
            AiPracticeScenarioLimit = aiPracticeScenarioLimit;
            ExpertEvaluationLimit = expertEvaluationLimit;
        }
    }
}
