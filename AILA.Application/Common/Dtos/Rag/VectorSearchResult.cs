using AILA.Domain.Entities;
using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class VectorSearchResult
{
    public float[] QueryEmbedding { get; set; } = Array.Empty<float>();
    public List<RagCitationDto> Citations { get; set; } = new();
    public List<(KnowledgeChunk Chunk, double SimilarityScore)> SimilarChunks { get; set; } = new();
    public string ContextText { get; set; } = string.Empty;
    public bool HasRelevantContent { get; set; }
}