using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.UpdateAccountResourceLimitOverride
{
    public class UpdateAccountResourceLimitOverrideRequest
    {
        public int? AiTokenLimit { get; set; }

        public int? AiPracticeScenarioLimit { get; set; }

        public int? ExpertEvaluationRequestLimit { get; set; }
    }
}
