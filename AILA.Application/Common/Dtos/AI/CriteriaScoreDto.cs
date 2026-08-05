using System.Text.Json.Serialization;

namespace AILA.Application.Common.Dtos.AI;

public class CriteriaScoreDto {
    public Guid? Id { get; set; } = Guid.NewGuid();
    public string CriteriaId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid SubmissionId { get; set; }

    public string CriteriaName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Evaluation { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Feedback { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Chuỗi hiển thị định dạng "20/50"</summary>
    public string DisplayScore => $"{Score}/{MaxScore}";
}
