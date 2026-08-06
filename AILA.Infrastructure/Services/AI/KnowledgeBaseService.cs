using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IKnowledgeChunkRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public KnowledgeBaseService(IKnowledgeChunkRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IndexDocumentResponseDto> IndexDocumentMaterialAsync(
        Guid materialId,
        Guid courseId,
        string materialTitle,
        string contentText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentText))
        {
            throw new ArgumentException("Nội dung tài liệu không được để trống.", nameof(contentText));
        }

        // Delete previous chunks if re-indexing
        await _repository.DeleteChunksByMaterialIdAsync(materialId, cancellationToken);

        var doc = new KnowledgeDocument(materialId, courseId);
        doc.MarkProcessing();
        await _repository.AddDocumentAsync(doc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var cleanText = CleanContentText(contentText);
            var chunksText = SplitTextIntoChunks(cleanText, maxWordsPerChunk: 150, overlapWords: 20);

            var chunkEntities = new List<KnowledgeChunk>();
            for (int i = 0; i < chunksText.Count; i++)
            {
                var chunkText = chunksText[i];
                var tokenCount = chunkText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var embedding = await GenerateEmbeddingAsync(chunkText, cancellationToken);

                var metadataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CourseId = courseId,
                    MaterialId = materialId,
                    MaterialTitle = materialTitle,
                    ChunkIndex = i + 1,
                    TotalChunks = chunksText.Count,
                    WordCount = tokenCount,
                    CharacterCount = chunkText.Length,
                    IndexedAt = DateTime.UtcNow
                });

                var chunk = new KnowledgeChunk(
                    doc.Id,
                    materialId,
                    courseId,
                    i + 1,
                    chunkText,
                    tokenCount,
                    embedding,
                    metadataJson);

                chunkEntities.Add(chunk);
            }

            await _repository.AddChunksAsync(chunkEntities, cancellationToken);
            doc.MarkCompleted(chunkEntities.Count);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new IndexDocumentResponseDto
            {
                KnowledgeDocumentId = doc.Id,
                MaterialId = materialId,
                CourseId = courseId,
                TotalChunks = chunkEntities.Count,
                Status = doc.Status.ToString()
            };
        }
        catch (Exception ex)
        {
            doc.MarkFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new IndexDocumentResponseDto
            {
                KnowledgeDocumentId = doc.Id,
                MaterialId = materialId,
                CourseId = courseId,
                TotalChunks = 0,
                Status = IndexingStatus.Failed.ToString()
            };
        }
    }

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Generates a 384-dimensional normalized feature vector based on SHA256 character n-gram hashing
        int vectorDim = 384;
        float[] vector = new float[vectorDim];
        if (string.IsNullOrWhiteSpace(text)) return Task.FromResult(vector);

        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        using var md5 = MD5.Create();

        foreach (var word in words)
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(word));
            int index = Math.Abs(BitConverter.ToInt32(hash, 0)) % vectorDim;
            float weight = (float)word.Length;
            vector[index] += weight;
        }

        // L2 Normalize
        double sumSquare = vector.Sum(v => v * v);
        if (sumSquare > 0)
        {
            float norm = (float)Math.Sqrt(sumSquare);
            for (int i = 0; i < vectorDim; i++)
            {
                vector[i] /= norm;
            }
        }

        return Task.FromResult(vector);
    }

    private static string CleanContentText(string rawText)
    {
        // Strip HTML tags & Markdown headers
        string noHtml = Regex.Replace(rawText, "<.*?>", " ");
        string cleanText = Regex.Replace(noHtml, @"[#*`_~]", " ");
        return Regex.Replace(cleanText, @"\s+", " ").Trim();
    }

    private static List<string> SplitTextIntoChunks(string text, int maxWordsPerChunk, int overlapWords)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();

        if (words.Length == 0) return chunks;

        int step = Math.Max(1, maxWordsPerChunk - overlapWords);
        for (int i = 0; i < words.Length; i += step)
        {
            var chunkWords = words.Skip(i).Take(maxWordsPerChunk);
            chunks.Add(string.Join(" ", chunkWords));

            if (i + maxWordsPerChunk >= words.Length) break;
        }

        return chunks;
    }
}
