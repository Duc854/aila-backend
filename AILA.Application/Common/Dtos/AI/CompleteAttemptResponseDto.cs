namespace AILA.Application.Common.Dtos.AI;

public class CompleteAttemptResponseDto
{
    public decimal FinalScore { get; set; }
    public string OverallSuggestion { get; set; } = string.Empty;
    public OverallScoringResult? DetailedScoring { get; set; }
}
