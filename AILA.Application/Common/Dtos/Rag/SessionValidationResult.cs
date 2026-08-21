using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class SessionValidationResult
{
    public bool IsValid { get; set; }
    public CourseChatSessionDto? Session { get; set; }
    public List<CourseChatMessageDto> RecentMessages { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? Status { get; set; }

    public static SessionValidationResult Valid(
        CourseChatSessionDto session,
        List<CourseChatMessageDto> recentMessages)
    {
        return new SessionValidationResult
        {
            IsValid = true,
            Session = session,
            RecentMessages = recentMessages
        };
    }

    public static SessionValidationResult Invalid(string errorMessage, string status = "Forbidden")
    {
        return new SessionValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Status = status
        };
    }
}