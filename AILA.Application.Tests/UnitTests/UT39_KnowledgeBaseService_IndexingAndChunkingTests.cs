using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using AILA.Infrastructure.Services.AI;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Unit Tests for KnowledgeBaseService:
/// - Text Cleaning (HTML/Markdown stripping)
/// - Text Chunking with Overlap
/// - Vector Embedding Generation (384-dim & L2 Normalization)
/// - Full Course Material Syncing
/// </summary>
public class UT39_KnowledgeBaseService_IndexingAndChunkingTests
{
    private readonly Mock<IKnowledgeChunkRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UT39_KnowledgeBaseService_IndexingAndChunkingTests()
    {
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private KnowledgeBaseService CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenTextValid_Returns384DimensionalL2NormalizedVector()
    {
        // Arrange
        var sut = CreateSut();
        var text = "Lập trình hướng đối tượng OOP và kiến trúc Clean Architecture";

        // Act
        var embedding = await sut.GenerateEmbeddingAsync(text);

        // Assert
        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);

        // L2 Norm phải xấp xỉ 1.0
        double sumSquare = embedding.Sum(v => v * v);
        Assert.InRange(Math.Sqrt(sumSquare), 0.99, 1.01);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenTextEmpty_ReturnsZeroVector()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var embedding = await sut.GenerateEmbeddingAsync(string.Empty);

        // Assert
        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);
        Assert.All(embedding, v => Assert.Equal(0f, v));
    }

    [Fact]
    public async Task IndexDocumentMaterialAsync_WhenValidText_GeneratesChunksAndMarksCompleted()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var content = "Đoạn văn bản hướng dẫn học phần REST API và cấu trúc dữ liệu.";

        var capturedChunks = new List<KnowledgeChunk>();
        _repository.Setup(x => x.AddChunksAsync(It.IsAny<IEnumerable<KnowledgeChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<KnowledgeChunk>, CancellationToken>((chunks, ct) => capturedChunks.AddRange(chunks))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.IndexDocumentMaterialAsync(materialId, courseId, "Bài 1: REST API", content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(materialId, result.MaterialId);
        Assert.Equal(courseId, result.CourseId);
        Assert.Equal("Completed", result.Status);
        Assert.True(result.TotalChunks >= 1);
        Assert.NotEmpty(capturedChunks);

        _repository.Verify(x => x.DeleteChunksByMaterialIdAsync(materialId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.AddDocumentAsync(It.IsAny<KnowledgeDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexDocumentMaterialAsync_WhenContentEmpty_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.IndexDocumentMaterialAsync(Guid.NewGuid(), Guid.NewGuid(), "Tiêu đề", "   "));
    }
}
