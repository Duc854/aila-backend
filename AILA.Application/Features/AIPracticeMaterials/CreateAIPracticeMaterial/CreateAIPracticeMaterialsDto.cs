using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterial.CreateAIPracticeMaterials
{
    public sealed class CreateAIPracticeMaterialRequestDto
    {
        // Material
        public Guid ModuleId { get; set; }

        public string Title { get; set; } = string.Empty;

        // AI Practice
        public string Scenario { get; set; } = string.Empty;

        public string AiTask { get; set; } = string.Empty;

        public string LearnerTask { get; set; } = string.Empty;

        public PracticeDifficulty Difficulty { get; set; }

        public int MaxPromptAttempts { get; set; }

        // Guidance
        public List<PromptTemplateDto> PromptTemplates { get; set; } = [];

        public List<StepGuidanceDto> StepGuidances { get; set; } = [];

        // Scoring
        public List<ScoringCriteriaDto> ScoringCriteria { get; set; } = [];
    }

    public sealed class PromptTemplateDto
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    public sealed class StepGuidanceDto
    {
        public int OrderIndex { get; set; }

        public string Content { get; set; } = string.Empty;
    }

    public sealed class ScoringCriteriaDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Weight { get; set; }
    }

    public sealed class AIPracticeMaterialDto
    {
        public Guid MaterialId { get; set; }

        public Guid ModuleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public PracticeDifficulty Difficulty { get; set; }

        public int MaxPromptAttempts { get; set; }
    }
}
