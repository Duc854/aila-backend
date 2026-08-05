using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories;

public interface IKnowledgeChunkRepository
{
    Task AddDocumentAsync(KnowledgeDocument doc, CancellationToken cancellationToken = default);
    Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default);
    Task<KnowledgeDocument?> GetDocumentByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default);
    Task DeleteChunksByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default);
    Task<List<(KnowledgeChunk Chunk, double SimilarityScore)>> SearchSimilarChunksAsync(Guid courseId, float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
    Task<CourseChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<CourseChatSession>> GetSessionsByAccountAndCourseAsync(Guid accountId, Guid courseId, CancellationToken cancellationToken = default);
    Task AddSessionAsync(CourseChatSession session, CancellationToken cancellationToken = default);
    Task AddMessageAsync(CourseChatMessage message, CancellationToken cancellationToken = default);
    Task<List<CourseChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> IsLearnerEnrolledInCourseAsync(Guid accountId, Guid courseId, CancellationToken cancellationToken = default);
}
