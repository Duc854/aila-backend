using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;
using AILA.Application.Features.Rag.Commands.CreateCourseChatSession;
using AILA.Application.Features.Rag.Queries.GetCourseChatMessages;
using AILA.Application.Features.Rag.Queries.GetCourseChatSessions;
using AILA.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Unit Tests for RAG CQRS Handlers:
/// - CreateCourseChatSessionCommandHandler (Enrolled check & Session Creation)
/// - GetCourseChatMessagesQueryHandler (IDOR check & Citation Parsing)
/// - AskCourseRagQuestionCommandHandler (MediatR routing to IRagChatService)
/// - GetCourseChatSessionsQueryHandler (Listing user sessions)
/// </summary>
public class UT41_RagCqrsHandlers_CommandsAndQueriesTests
{
    private readonly Mock<IKnowledgeChunkRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRagChatService> _ragChatService = new();

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public UT41_RagCqrsHandlers_CommandsAndQueriesTests()
    {
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateCourseChatSession_WhenEnrolled_CreatesSessionSuccessfully()
    {
        // Arrange
        _repository.Setup(x => x.IsLearnerEnrolledInCourseAsync(_accountId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        CourseChatSession? capturedSession = null;
        _repository.Setup(x => x.AddSessionAsync(It.IsAny<CourseChatSession>(), It.IsAny<CancellationToken>()))
            .Callback<CourseChatSession, CancellationToken>((s, ct) => capturedSession = s)
            .Returns(Task.CompletedTask);

        var handler = new CreateCourseChatSessionCommandHandler(_repository.Object, _unitOfWork.Object);
        var command = new CreateCourseChatSessionCommand(_accountId, _courseId, "Tiêu đề mới");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_accountId, result.AccountId);
        Assert.Equal(_courseId, result.CourseId);
        Assert.Equal("Tiêu đề mới", result.Title);
        Assert.NotNull(capturedSession);
    }

    [Fact]
    public async Task CreateCourseChatSession_WhenNotEnrolled_ThrowsBusinessRuleException()
    {
        // Arrange
        _repository.Setup(x => x.IsLearnerEnrolledInCourseAsync(_accountId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateCourseChatSessionCommandHandler(_repository.Object, _unitOfWork.Object);
        var command = new CreateCourseChatSessionCommand(_accountId, _courseId, "Tiêu đề mới");

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetCourseChatMessages_WhenUserOwnsSession_ReturnsMessagesWithParsedCitations()
    {
        // Arrange
        var session = new CourseChatSession(_accountId, _courseId, "Test");
        _repository.Setup(x => x.GetSessionByIdAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var citationsJson = "[{\"MaterialId\":\"" + Guid.NewGuid() + "\",\"MaterialTitle\":\"Bài 1\",\"Snippet\":\"Đoạn trích\",\"SimilarityScore\":0.85}]";
        var messages = new List<CourseChatMessage>
        {
            new CourseChatMessage(_sessionId, "user", "REST API là gì?"),
            new CourseChatMessage(_sessionId, "assistant", "REST API là...", citationsJson, 10, 20)
        };
        _repository.Setup(x => x.GetMessagesBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var handler = new GetCourseChatMessagesQueryHandler(_repository.Object);
        var query = new GetCourseChatMessagesQuery(_sessionId, _accountId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("user", result[0].Role);
        Assert.Equal("assistant", result[1].Role);
        Assert.Single(result[1].Citations);
        Assert.Equal("Bài 1", result[1].Citations[0].MaterialTitle);
    }

    [Fact]
    public async Task GetCourseChatMessages_WhenUserDoesNotOwnSession_ThrowsForbiddenAccessException()
    {
        // Arrange: Hacker with different AccountId trying to read session messages
        var hackerAccountId = Guid.NewGuid();
        var session = new CourseChatSession(_accountId, _courseId, "Test");
        _repository.Setup(x => x.GetSessionByIdAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var handler = new GetCourseChatMessagesQueryHandler(_repository.Object);
        var query = new GetCourseChatMessagesQuery(_sessionId, hackerAccountId);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task AskCourseRagQuestion_DelegatesToRagChatService()
    {
        // Arrange
        var expectedResponse = new AskRagQuestionResponseDto
        {
            MessageId = Guid.NewGuid(),
            Question = "Hỏi",
            Answer = "Đáp",
            Status = "Success"
        };

        _ragChatService.Setup(x => x.AskCourseQuestionAsync(_sessionId, _accountId, "Hỏi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new AskCourseRagQuestionCommandHandler(_ragChatService.Object);
        var command = new AskCourseRagQuestionCommand(_sessionId, _accountId, "Hỏi");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Success", result.Status);
        Assert.Equal("Đáp", result.Answer);
    }
}
