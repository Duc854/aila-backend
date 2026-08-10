using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class CourseChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<RagCitationDto> Citations { get; set; } = new();
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public DateTime CreatedAt { get; set; }
}
