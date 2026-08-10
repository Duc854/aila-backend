using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail
{
    public sealed class AIPracticeMaterialDetailDto
    {
        public Guid MaterialId { get; set; }

        public Guid ModuleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Scenario { get; set; } = string.Empty;

        public string AiTask { get; set; } = string.Empty;

        public string LearnerTask { get; set; } = string.Empty;

        public PracticeDifficulty Difficulty { get; set; }

        public int MaxPromptAttempts { get; set; }

        public List<PromptTemplateDto> PromptTemplates { get; set; } = [];

        public List<StepGuidanceDto> StepGuidances { get; set; } = [];

        public List<ScoringCriteriaDto> ScoringCriteria { get; set; } = [];
    }
}
