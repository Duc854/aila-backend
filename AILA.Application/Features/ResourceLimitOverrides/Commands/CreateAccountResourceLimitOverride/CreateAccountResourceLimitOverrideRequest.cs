using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.CreateAccountResourceLimitOverride
{
    public class CreateAccountResourceLimitOverrideRequest
    {
        public Guid AccountId { get; set; }

        public int? AiTokenLimit { get; set; }

        public int? AiPracticeScenarioLimit { get; set; }

        public int? ExpertEvaluationRequestLimit { get; set; }
    }
}
