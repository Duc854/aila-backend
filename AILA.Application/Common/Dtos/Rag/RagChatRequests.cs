using System;

namespace AILA.Application.Common.Dtos.Rag;

public class CreateSessionRequest
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;
}
