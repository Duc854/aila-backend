using System;

namespace AILA.Application.Common.Dtos.Rag;

public class CourseChatSessionDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
