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

    /// <summary>
    /// UC-29/30: id yêu cầu nhờ chuyên gia đánh giá còn hiệu lực của lượt này.
    /// Null nghĩa là chưa gửi yêu cầu (hoặc yêu cầu đã hủy) — FE hiện nút "Nhờ chuyên gia
    /// đánh giá"; có giá trị thì FE đổi thành link xem đánh giá của chuyên gia.
    /// </summary>
    public Guid? ExpertEvaluationRequestId { get; set; }

    /// <summary>
    /// Trạng thái của yêu cầu trên: "Pending" | "InProgress" | "Completed".
    /// Null khi <see cref="ExpertEvaluationRequestId"/> null.
    /// </summary>
    public string? ExpertEvaluationStatus { get; set; }


    public List<PromptSubmissionDto> Submissions { get; set; } = new();
}
