using System.Text.Json.Serialization;

namespace AILA.Application.Common.Dtos.AI;

public class PracticeAttemptDto 
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid MaterialId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? FinalScore { get; set; }
    
    /// <summary>Gợi ý cải thiện tổng thể sau khi hoàn thành bài thực hành</summary>
    public string OverallSuggestion { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OverallScoringResult? DetailedScoring { get; set; }
    
    public List<PromptSubmissionDto> Submissions { get; set; } = new();
}
