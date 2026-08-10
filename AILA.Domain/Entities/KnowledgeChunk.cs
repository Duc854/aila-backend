using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities;

public class KnowledgeChunk : BaseEntity
{
    public Guid KnowledgeDocumentId { get; private set; }
    public Guid MaterialId { get; private set; }
    public Guid CourseId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }
    public float[] Embedding { get; private set; } = Array.Empty<float>();
    public string? MetadataJson { get; private set; }

    public virtual KnowledgeDocument KnowledgeDocument { get; private set; } = null!;

    private KnowledgeChunk() { }

    public KnowledgeChunk(
        Guid knowledgeDocumentId,
        Guid materialId,
        Guid courseId,
        int chunkIndex,
        string content,
        int tokenCount,
        float[] embedding,
        string? metadataJson = null)
    {
        Id = Guid.NewGuid();
        KnowledgeDocumentId = knowledgeDocumentId;
        MaterialId = materialId;
        CourseId = courseId;
        ChunkIndex = chunkIndex;
        Content = content;
        TokenCount = tokenCount;
        Embedding = embedding;
        MetadataJson = metadataJson;
    }
}
