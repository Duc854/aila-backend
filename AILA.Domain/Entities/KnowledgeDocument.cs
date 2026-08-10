using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;

namespace AILA.Domain.Entities;

public class KnowledgeDocument : BaseEntity
{
    public Guid MaterialId { get; private set; }
    public Guid CourseId { get; private set; }
    public IndexingStatus Status { get; private set; }
    public int TotalChunks { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? LastIndexedAt { get; private set; }

    private readonly List<KnowledgeChunk> _chunks = new();
    public virtual IReadOnlyCollection<KnowledgeChunk> Chunks => _chunks.AsReadOnly();

    private KnowledgeDocument() { }

    public KnowledgeDocument(Guid materialId, Guid courseId)
    {
        Id = Guid.NewGuid();
        MaterialId = materialId;
        CourseId = courseId;
        Status = IndexingStatus.Pending;
        TotalChunks = 0;
    }

    public void MarkProcessing()
    {
        Status = IndexingStatus.Processing;
        ErrorMessage = null;
        UpdateTimestamp();
    }

    public void MarkCompleted(int totalChunks)
    {
        Status = IndexingStatus.Completed;
        TotalChunks = totalChunks;
        LastIndexedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkFailed(string errorMessage)
    {
        Status = IndexingStatus.Failed;
        ErrorMessage = errorMessage;
        UpdateTimestamp();
    }
}
