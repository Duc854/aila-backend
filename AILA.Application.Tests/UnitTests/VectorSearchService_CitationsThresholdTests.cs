using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Infrastructure.Services.AI;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT37_RagCitationsThreshold — <see cref="VectorSearchService.SearchAsync"/>
/// Module: AI / RAG Chatbot · 2 test case
///
/// Phạm vi: chỉ nhánh trích dẫn —
///        B1 = truy vấn KHÔNG có chunk nào vượt ngưỡng cosine 0.60 ⇒ Citations rỗng
///        B2 = có chunk vượt ngưỡng ⇒ Citations lấy MaterialTitle từ MetadataJson và giữ nguyên score thật
/// Các nhánh còn lại (PII, moderation, quota, IDOR, enrollment, retry 429) thuộc các sheet khác.
///
/// GHI CHÚ REFACTOR: logic dựng trích dẫn trước đây nằm trong RagChatService.AskCourseQuestionAsync.
/// Sau khi RagChatService được tách thành các collaborator (ISessionValidator, IVectorSearchService,
/// IPromptBuilder, IChatResponseHandler, IMessagePersistence), toàn bộ phần lọc ngưỡng + dựng
/// RagCitationDto chuyển sang VectorSearchService.SearchAsync — nên sheet này bám theo SUT mới.
/// Bản test cũ vẫn mock IKnowledgeChunkRepository (interface đã đổi tên thành IKnowledgeRepository)
/// nên không còn biên dịch được.
///
/// PHỤ THUỘC BẮT BUỘC PHẢI STUB:
///   · GenerateEmbeddingAsync — nếu thiếu, Moq trả null và repository nhận embedding rỗng.
/// </summary>
public class UT37_VectorSearchService_CitationsThresholdTests
{
    private readonly Mock<IKnowledgeRepository> _repository = new();
    private readonly Mock<IKnowledgeBaseService> _knowledgeBaseService = new();

    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _materialId = Guid.NewGuid();

    public UT37_VectorSearchService_CitationsThresholdTests()
    {
        _knowledgeBaseService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });
    }

    private VectorSearchService CreateSut() => new(
        _repository.Object,
        _knowledgeBaseService.Object);

    private void WithSimilarChunks(params (KnowledgeChunk Chunk, double SimilarityScore)[] chunks)
    {
        _repository.Setup(x => x.SearchSimilarChunksAsync(
            _courseId,
            It.IsAny<float[]>(),
            It.IsAny<int>(),
            0.60,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(KnowledgeChunk Chunk, double SimilarityScore)>(chunks));
    }

    /// <summary>
    /// UTCID01 · B1=T · Type N — câu chào xã giao "hello", không chunk nào vượt ngưỡng 0.60.
    /// Kết quả tìm kiếm TUYỆT ĐỐI không được đính kèm trích dẫn nào.
    /// </summary>
    [Fact]
    public async Task UTCID01_NoChunkAboveThreshold_ReturnsZeroCitations()
    {
        // Arrange: "hello" query produces 0 similar chunks above 0.60 threshold
        WithSimilarChunks();

        var sut = CreateSut();

        // Act
        var result = await sut.SearchAsync(_courseId, "hello", topK: 3, minSimilarity: 0.60);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Citations); // <-- Must have ZERO citations!
        Assert.False(result.HasRelevantContent);
        Assert.Equal(string.Empty, result.ContextText);

        // Ngưỡng tương tự và topK phải được truyền xuống tầng truy vấn, không để mặc định.
        _repository.Verify(x => x.SearchSimilarChunksAsync(
            _courseId, It.IsAny<float[]>(), 3, 0.60, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID02 · B2=T · Type N — câu hỏi đúng nội dung khóa học, 1 chunk đạt similarity 0.85.
    /// Citation phải lấy MaterialTitle từ MetadataJson và giữ nguyên điểm tương tự thật.
    /// </summary>
    [Fact]
    public async Task UTCID02_ChunkAboveThreshold_ReturnsCitationWithRealScore()
    {
        // Arrange: Relevant course question produces 1 chunk with 0.85 similarity
        var doc = new KnowledgeDocument(_materialId, _courseId);
        var chunk = new KnowledgeChunk(
            doc.Id,
            _materialId,
            _courseId,
            0,
            "REST API là tiêu chuẩn kiến trúc phần mềm sử dụng HTTP methods.",
            100,
            new float[] { 0.1f, 0.2f, 0.3f },
            "{\"MaterialTitle\":\"Bài 1: Giới thiệu REST API\"}");

        WithSimilarChunks((chunk, 0.85));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchAsync(_courseId, "REST API là gì?", topK: 3, minSimilarity: 0.60);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasRelevantContent);
        Assert.Single(result.Citations); // <-- Exactly 1 citation returned
        Assert.Equal("Bài 1: Giới thiệu REST API", result.Citations[0].MaterialTitle);
        Assert.Equal(0.85, result.Citations[0].SimilarityScore);
        Assert.Equal(_materialId, result.Citations[0].MaterialId);
        Assert.Contains("Bài 1: Giới thiệu REST API", result.ContextText);
    }
}
