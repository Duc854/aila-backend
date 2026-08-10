using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos
{
    public sealed class ResourceLimitPolicyDto
    {
        public ResourceAccountType AccountType { get; set; }

        public int AiTokenLimit { get; set; }

        public int AiPracticeScenarioLimit { get; set; }

        public int ExpertEvaluationRequestLimit { get; set; }
    }
}
