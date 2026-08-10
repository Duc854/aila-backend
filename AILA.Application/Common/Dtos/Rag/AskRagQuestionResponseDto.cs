using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class AskRagQuestionResponseDto
{
    public Guid MessageId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<RagCitationDto> Citations { get; set; } = new();
    public string Status { get; set; } = "Success";
    public string? WarningMessage { get; set; }
}
