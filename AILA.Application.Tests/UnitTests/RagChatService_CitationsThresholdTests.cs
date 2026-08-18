using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Infrastructure.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Unit Tests verifying that RAG Chatbot does NOT return citations for low-similarity queries (e.g. "hello", "hi")
/// and ONLY includes citations when cosine similarity is above the threshold (>= 0.60).
/// </summary>
public class UT37_RagChatService_CitationsThresholdTests
{
    private readonly Mock<IKnowledgeChunkRepository> _repository = new();
    private readonly Mock<IKnowledgeBaseService> _knowledgeBaseService = new();
    private readonly Mock<IQuotaService> _quotaService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IChatCompletionService> _chatCompletion = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<IPrivacyService> _privacyService = new();
    private readonly Mock<IModerationService> _moderationService = new();

    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _materialId = Guid.NewGuid();

    public UT37_RagChatService_CitationsThresholdTests()
    {
        _privacyService.Setup(x => x.MaskSensitiveData(It.IsAny<string>())).Returns<string>(s => s);
        _privacyService.Setup(x => x.HasSensitiveData(It.IsAny<string>())).Returns(false);
        _moderationService.Setup(x => x.CheckContentSafetyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));
        _quotaService.Setup(x => x.CheckQuotaAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaCheckResultDto { IsAllowed = true });

        var session = new CourseChatSession(_accountId, _courseId, "Test Session");
        _repository.Setup(x => x.GetSessionByIdAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _repository.Setup(x => x.GetMessagesBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseChatMessage>());

        _knowledgeBaseService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        _chatCompletion.Setup(x => x.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { new ChatMessageContent(AuthorRole.Assistant, "Chào bạn! Tôi có thể giúp gì cho bạn về khóa học này?") });

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RagChatService CreateSut() => new(
        _repository.Object,
        _knowledgeBaseService.Object,
        _quotaService.Object,
        _unitOfWork.Object,
        _chatCompletion.Object,
        _configuration.Object,
        _privacyService.Object,
        _moderationService.Object);

    [Fact]
    public async Task AskCourseQuestion_WhenUserSaysHello_NoChunksAboveThreshold_ReturnsZeroCitations()
    {
        // Arrange: "hello" query produces 0 similar chunks above 0.60 threshold
        _repository.Setup(x => x.SearchSimilarChunksAsync(
            _courseId,
            It.IsAny<float[]>(),
            It.IsAny<int>(),
            0.60,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(KnowledgeChunk Chunk, double SimilarityScore)>());

        var sut = CreateSut();

        // Act
        var result = await sut.AskCourseQuestionAsync(_sessionId, _accountId, "hello");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Success", result.Status);
        Assert.Empty(result.Citations); // <-- Must have ZERO citations!
        Assert.Equal("Chào bạn! Tôi có thể giúp gì cho bạn về khóa học này?", result.Answer);
    }

    [Fact]
    public async Task AskCourseQuestion_WhenUserAsksRelevantQuestion_ChunksAboveThreshold_ReturnsCitations()
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

        _repository.Setup(x => x.SearchSimilarChunksAsync(
            _courseId,
            It.IsAny<float[]>(),
            It.IsAny<int>(),
            0.60,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(KnowledgeChunk Chunk, double SimilarityScore)>
            {
                (chunk, 0.85)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.AskCourseQuestionAsync(_sessionId, _accountId, "REST API là gì?");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Success", result.Status);
        Assert.Single(result.Citations); // <-- Exactly 1 citation returned
        Assert.Equal("Bài 1: Giới thiệu REST API", result.Citations[0].MaterialTitle);
        Assert.Equal(0.85, result.Citations[0].SimilarityScore);
    }
}
