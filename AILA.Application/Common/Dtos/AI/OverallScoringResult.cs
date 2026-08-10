using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.AI;

public class OverallScoringResult
{
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public decimal Percentage { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<CriteriaScoreDto> Criteria { get; set; } = new();
    public List<string> DetectedIssues { get; set; } = new();
    public List<string> LearningSuggestions { get; set; } = new();
    public string NextPromptExample { get; set; } = string.Empty;
    public MetadataDto Metadata { get; set; } = new();
}

public class MetadataDto
{
    public int ConversationAnalyzed { get; set; }
    public int ValidPrompts { get; set; }
    public int InvalidPrompts { get; set; }
    public bool ScenarioUsed { get; set; }
    public bool UserTaskCompleted { get; set; }
}
