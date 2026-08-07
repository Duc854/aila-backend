using System.Text.Json.Serialization;

namespace AILA.Application.Common.Dtos.AI;

public class PromptSubmissionDto 
{
    public Guid Id { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    
    /// <summary>Trạng thái lượt submit: "Success", "ValidationError", "Violation"</summary>
    public string Status { get; set; } = "Success";
    
    public bool IsViolation { get; set; } = false;
    public string? ViolationMessage { get; set; }
    public string? WarningMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Chỉ xuất hiện trong JSON khi đã hoàn thành/chấm điểm bài tập</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CriteriaScoreDto>? CriteriaScores { get; set; }

    /// <summary>Chỉ xuất hiện trong JSON khi đã hoàn thành/chấm điểm bài tập</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TotalScore { get; set; }
}
