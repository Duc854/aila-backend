using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.PracticeAttempts.Commands.AbandonAttempt;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

public class AbandonAttempt_HandleTests
{
    private readonly Mock<IPracticeAttemptRepository> _repository = new();
    private readonly Mock<IAIPracticeMaterialRepository> _materialRepo = new();
    private readonly Mock<IScoringService> _scoringService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();

    private readonly Guid _attemptId = Guid.NewGuid();
    private readonly Guid _materialId = Guid.NewGuid();
    private readonly Guid _enrollmentId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();

    public AbandonAttempt_HandleTests()
    {
        _unitOfWork.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AbandonAttemptCommandHandler CreateSut() => new(
        _repository.Object,
        _materialRepo.Object,
        _scoringService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_AttemptNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PracticeAttempt?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new AbandonAttemptCommand(_attemptId, _learnerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IDOR_DifferentLearner_ThrowsForbiddenAccessException()
    {
        var attempt = new PracticeAttempt(_enrollmentId, _materialId);
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);

        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId))
            .ReturnsAsync(enrollment);

        var attackerId = Guid.NewGuid();
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.Handle(new AbandonAttemptCommand(_attemptId, attackerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoSubmissions_AbandonsAttemptDirectly()
    {
        var attempt = new PracticeAttempt(_enrollmentId, _materialId);
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);

        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId))
            .ReturnsAsync(enrollment);

        var sut = CreateSut();
        await sut.Handle(new AbandonAttemptCommand(_attemptId, _learnerId), CancellationToken.None);

        Assert.Equal(PracticeAttemptStatus.Abandoned, attempt.Status);
        _scoringService.Verify(x => x.GenerateOverallSuggestionAsync(
            It.IsAny<List<PromptSubmission>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<ScoringCriteria>>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSubmissions_LoadsFullCriteriaAndScoresAttempt()
    {
        var attempt = new PracticeAttempt(_enrollmentId, _materialId);
        attempt.AddSubmission("User prompt", "AI response");

        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);
        var material = new AIPracticeMaterial(_materialId, "Scenario", "AITask", "LearnerTask", PracticeDifficulty.Easy, 3);
        var criteria = new ScoringCriteria(Guid.NewGuid(), "Tiêu chí 1", "Mô tả", 100);
        material.AddScoringCriteria(criteria);

        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId))
            .ReturnsAsync(enrollment);
        _materialRepo.Setup(x => x.GetByIdWithDetailsAsync(_materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(material);

        var scoringResult = new OverallScoringResult
        {
            Percentage = 75,
            Summary = "Đạt yêu cầu cơ bản"
        };

        _scoringService.Setup(x => x.GenerateOverallSuggestionAsync(
                It.IsAny<List<PromptSubmission>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<List<ScoringCriteria>>(c => c.Count == 1),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(scoringResult);

        var sut = CreateSut();
        await sut.Handle(new AbandonAttemptCommand(_attemptId, _learnerId), CancellationToken.None);

        Assert.Equal(PracticeAttemptStatus.Completed, attempt.Status);
        Assert.Equal(75, attempt.FinalScore);
        Assert.Equal("Đạt yêu cầu cơ bản", attempt.OverallSuggestion);
        _materialRepo.Verify(x => x.GetByIdWithDetailsAsync(_materialId, It.IsAny<CancellationToken>()), Times.Once);
    }
}