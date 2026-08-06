using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Commands.UpdateAIPracticeMaterial
{
    public sealed class UpdateAIPracticeMaterialDto
    {
        public string Title { get; set; } = string.Empty;

        public string Scenario { get; set; } = string.Empty;

        public string AiTask { get; set; } = string.Empty;

        public string LearnerTask { get; set; } = string.Empty;

        public int MaxPromptAttempts { get; set; }

        public List<PromptTemplateDto> PromptTemplates { get; set; } = [];

        public List<StepGuidanceDto> StepGuidances { get; set; } = [];

        public List<ScoringCriteriaDto> ScoringCriteria { get; set; } = [];
    }
}
