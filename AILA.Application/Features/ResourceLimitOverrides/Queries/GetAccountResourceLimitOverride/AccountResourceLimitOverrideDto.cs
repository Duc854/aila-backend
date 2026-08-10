using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Queries.GetAccountResourceLimitOverride
{
    public class AccountResourceLimitOverrideDto
    {
        public Guid AccountId { get; set; }

        public bool HasOverride { get; set; }

        public int? AiTokenLimit { get; set; }

        public int? AiPracticeScenarioLimit { get; set; }

        public int? ExpertEvaluationRequestLimit { get; set; }
    }
}
