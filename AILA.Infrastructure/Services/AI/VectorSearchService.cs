using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class VectorSearchService : IVectorSearchService
{
    private readonly IKnowledgeRepository _repository;
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public VectorSearchService(
        IKnowledgeRepository repository,
        IKnowledgeBaseService knowledgeBaseService)
    {
        _repository = repository;
        _knowledgeBaseService = knowledgeBaseService;
    }

    public async Task<VectorSearchResult> SearchAsync(
        Guid courseId,
        string question,
        int topK = 3,
        double minSimilarity = 0.60,
        CancellationToken cancellationToken = default)
    {
        // 1. Generate embedding cho câu hỏi
        var queryEmbedding = await _knowledgeBaseService.GenerateEmbeddingAsync(question, cancellationToken);

        // 2. Tìm kiếm các chunk tương tự - Gọi repository
        var similarChunks = await _repository.SearchSimilarChunksAsync(
            courseId,
            queryEmbedding,
            topK,
            minSimilarity,
            cancellationToken);

        // 3. Build citations và context text
        var citations = new List<RagCitationDto>();
        var contextText = new StringBuilder();
        var hasRelevantContent = similarChunks.Any();

        if (hasRelevantContent)
        {
            foreach (var (chunk, score) in similarChunks)
            {
                // Lấy title từ metadata
                string title = ExtractTitleFromMetadata(chunk);

                // Tạo snippet
                var snippet = chunk.Content.Length > 150
                    ? chunk.Content.Substring(0, 150) + "..."
                    : chunk.Content;

                citations.Add(new RagCitationDto
                {
                    MaterialId = chunk.MaterialId,
                    MaterialTitle = title,
                    Snippet = snippet,
                    SimilarityScore = score
                });

                contextText.AppendLine($"--- [Trích dẫn từ bài học: {title}] ---");
                contextText.AppendLine(chunk.Content);
                contextText.AppendLine();
            }
        }

        return new VectorSearchResult
        {
            QueryEmbedding = queryEmbedding,
            Citations = citations,
            SimilarChunks = similarChunks,
            ContextText = contextText.ToString(),
            HasRelevantContent = hasRelevantContent
        };
    }

    private static string ExtractTitleFromMetadata(KnowledgeChunk chunk)
    {
        string title = $"Bài học #{chunk.ChunkIndex}";

        if (!string.IsNullOrWhiteSpace(chunk.MetadataJson))
        {
            try
            {
                using var metaDoc = JsonDocument.Parse(chunk.MetadataJson);
                if (metaDoc.RootElement.TryGetProperty("MaterialTitle", out var tProp))
                {
                    title = tProp.GetString() ?? title;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse, giữ title mặc định
            }
        }

        return title;
    }
}