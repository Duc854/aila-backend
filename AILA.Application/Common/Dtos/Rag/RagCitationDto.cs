using System;

namespace AILA.Application.Common.Dtos.Rag;

public class RagCitationDto
{
    public Guid MaterialId { get; set; }
    public string MaterialTitle { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
