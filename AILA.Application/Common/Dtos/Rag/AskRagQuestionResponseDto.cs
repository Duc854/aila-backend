using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class AskRagQuestionResponseDto
{
    public Guid MessageId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<RagCitationDto> Citations { get; set; } = new();
    public string Status { get; set; } = "Success"; // "Success", "Violation", "ValidationError", "QuotaExceeded"
    public bool IsViolation { get; set; }
    public string? ViolationMessage { get; set; }
    public string? WarningMessage { get; set; }
}
