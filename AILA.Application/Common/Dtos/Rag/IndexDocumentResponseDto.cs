using System;

namespace AILA.Application.Common.Dtos.Rag;

public class IndexDocumentResponseDto
{
    public Guid KnowledgeDocumentId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid CourseId { get; set; }
    public int TotalChunks { get; set; }
    public string Status { get; set; } = string.Empty;
}
