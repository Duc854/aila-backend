using AILA.Domain.Enums;
using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.AI;

public class AIPracticeMaterialDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public PracticeDifficulty Difficulty { get; set; }
    public int MaxPromptAttempts { get; set; }
    public List<PromptTemplateDto> PromptTemplates { get; set; } = new();
    public List<StepGuidanceDto> StepGuidances { get; set; } = new();
}

public class PromptTemplateDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class StepGuidanceDto
{
    public Guid Id { get; set; }
    public int StepOrder { get; set; }
    public string StepTitle { get; set; } = string.Empty;
    public string GuidanceText { get; set; } = string.Empty;
}
