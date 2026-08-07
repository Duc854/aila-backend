using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using AILA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories;

public class KnowledgeChunkRepository : IKnowledgeChunkRepository
{
    private readonly ApplicationDbContext _context;

    public KnowledgeChunkRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddDocumentAsync(KnowledgeDocument doc, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeDocuments.AddAsync(doc, cancellationToken);
    }

    public async Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeChunks.AddRangeAsync(chunks, cancellationToken);
    }

    public async Task<KnowledgeDocument?> GetDocumentByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.MaterialId == materialId, cancellationToken);
    }

    public async Task DeleteChunksByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        var doc = await GetDocumentByMaterialIdAsync(materialId, cancellationToken);
        if (doc != null)
        {
            var oldChunks = await _context.KnowledgeChunks
                .Where(c => c.KnowledgeDocumentId == doc.Id)
                .ToListAsync(cancellationToken);

            _context.KnowledgeChunks.RemoveRange(oldChunks);
            _context.KnowledgeDocuments.Remove(doc);
        }
    }

    public async Task<List<(KnowledgeChunk Chunk, double SimilarityScore)>> SearchSimilarChunksAsync(Guid courseId, float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        var chunks = await _context.KnowledgeChunks
            .Where(x => x.CourseId == courseId)
            .ToListAsync(cancellationToken);

        if (!chunks.Any())
            return new List<(KnowledgeChunk Chunk, double SimilarityScore)>();

        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            return chunks.Take(topK).Select(c => (c, 0.85)).ToList();
        }

        return chunks
            .Select(c => (
                Chunk: c,
                Similarity: CalculateCosineSimilarity(queryEmbedding, c.Embedding)
            ))
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => (x.Chunk, Math.Round(x.Similarity, 4)))
            .ToList();
    }

    public async Task<CourseChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task<List<CourseChatSession>> GetSessionsByAccountAndCourseAsync(Guid accountId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseChatSessions
            .Where(s => s.AccountId == accountId && s.CourseId == courseId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSessionAsync(CourseChatSession session, CancellationToken cancellationToken = default)
    {
        await _context.CourseChatSessions.AddAsync(session, cancellationToken);
    }

    public async Task AddMessageAsync(CourseChatMessage message, CancellationToken cancellationToken = default)
    {
        await _context.CourseChatMessages.AddAsync(message, cancellationToken);
    }

    public async Task<List<CourseChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsLearnerEnrolledInCourseAsync(Guid accountId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.LearnerId == accountId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active, cancellationToken);

        return enrollment != null;
    }

    private static double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length || vectorA.Length == 0)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
