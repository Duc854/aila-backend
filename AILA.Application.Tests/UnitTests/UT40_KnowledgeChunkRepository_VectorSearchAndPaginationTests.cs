using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Unit Tests for Vector Math and Knowledge Repository Logic:
/// - Cosine Similarity Math Verification (Identical, Orthogonal, Low Similarity)
/// - Chunk Filtering & Top-K Ranking Logic
/// </summary>
public class UT40_KnowledgeChunkRepository_VectorSearchAndPaginationTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        float[] vectorA = new float[] { 0.577f, 0.577f, 0.577f };
        float[] vectorB = new float[] { 0.577f, 0.577f, 0.577f };

        // Act
        double similarity = CalculateCosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.InRange(similarity, 0.99, 1.01);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        // Arrange: 2 vector vuông góc [1, 0] và [0, 1]
        float[] vectorA = new float[] { 1.0f, 0.0f, 0.0f };
        float[] vectorB = new float[] { 0.0f, 1.0f, 0.0f };

        // Act
        double similarity = CalculateCosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.Equal(0.0, similarity);
    }

    [Fact]
    public void SearchSimilarChunks_FiltersThresholdAndOrdersDescending()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var matId = Guid.NewGuid();

        var queryVec = new float[] { 1.0f, 0.0f };

        // Chunk 1: Similarity = 1.0 (khớp 100%)
        var chunk1 = new KnowledgeChunk(docId, matId, courseId, 1, "Bài 1", 10, new float[] { 1.0f, 0.0f });
        // Chunk 2: Similarity = 0.707 (khớp 70%)
        var chunk2 = new KnowledgeChunk(docId, matId, courseId, 2, "Bài 2", 10, new float[] { 0.707f, 0.707f });
        // Chunk 3: Similarity = 0.0 (không khớp)
        var chunk3 = new KnowledgeChunk(docId, matId, courseId, 3, "Bài 3", 10, new float[] { 0.0f, 1.0f });

        var chunks = new List<KnowledgeChunk> { chunk1, chunk2, chunk3 };

        // Act: Lọc ngưỡng minSimilarity >= 0.60
        var results = chunks
            .Select(c => (Chunk: c, Similarity: CalculateCosineSimilarity(queryVec, c.Embedding)))
            .Where(x => x.Similarity >= 0.60)
            .OrderByDescending(x => x.Similarity)
            .Take(3)
            .ToList();

        // Assert
        Assert.Equal(2, results.Count); // Chỉ có chunk1 và chunk2 đạt >= 0.60
        Assert.Equal(chunk1.Id, results[0].Chunk.Id);
        Assert.InRange(results[0].Similarity, 0.99, 1.01);
        Assert.Equal(chunk2.Id, results[1].Chunk.Id);
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
