// Application/Common/Dtos/AI/PromptViolationLogDto.cs
namespace AILA.Application.Common.Dtos.AI;

public class PromptViolationLogDto
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public string ViolationReason { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
