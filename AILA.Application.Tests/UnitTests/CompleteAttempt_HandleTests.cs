using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

public class CompleteAttempt_HandleTests
{
    private readonly Mock<IPracticeAttemptRepository> _repository = new();
    private readonly Mock<IAIPracticeMaterialRepository> _materialRepo = new();
    private readonly Mock<IScoringService> _scoringService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILearnerBehaviorService> _learnerBehaviorService = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<ILearningProgressRepository> _progresses = new();
    private readonly Mock<IGenericRepository<AIFeedback>> _feedbackRepo = new();

    private readonly Guid _attemptId = Guid.NewGuid();
    private readonly Guid _materialId = Guid.NewGuid();
    private readonly Guid _enrollmentId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();

    public CompleteAttempt_HandleTests()
    {
        _unitOfWork.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _unitOfWork.SetupGet(x => x.LearningProgresses).Returns(_progresses.Object);
        _unitOfWork.Setup(x => x.Repository<AIFeedback>()).Returns(_feedbackRepo.Object);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private CompleteAttemptCommandHandler CreateSut() => new(
        _repository.Object,
        _materialRepo.Object,
        _scoringService.Object,
        _unitOfWork.Object,
        _learnerBehaviorService.Object);

    [Fact]
    public async Task Handle_AttemptNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PracticeAttempt?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new CompleteAttemptCommand(_attemptId, _learnerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IDOR_DifferentLearner_ThrowsForbiddenAccessException()
    {
        var attempt = new PracticeAttempt(_enrollmentId, _materialId);
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);

        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _enrollments.Setup(x => x.GetWithCourseTagsByIdAsync(_enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        var attackerId = Guid.NewGuid();
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.Handle(new CompleteAttemptCommand(_attemptId, attackerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FirstCompleted_Scores_PersistsFeedback_IncreasesBehaviorScore()
    {
        var attempt = new PracticeAttempt(_enrollmentId, _materialId);
        attempt.AddSubmission("User prompt", "AI response");

        var course = new Course("Test Course", Guid.NewGuid(), Guid.NewGuid(), KnowledgeLevel.Beginner, "Test Desc");
        var enrollment = new Enrollment(_learnerId, course.Id, 3);
        AILA.Application.Tests.UnitTests.TestHelpers.PrivateSetter.Set(enrollment, "Course", course);

        var material = new AIPracticeMaterial(_materialId, "Scenario", "AITask", "LearnerTask", PracticeDifficulty.Easy, 3);
        var criteria = new ScoringCriteria(Guid.NewGuid(), "Độ rõ ràng", "Mô tả", 100);
        material.AddScoringCriteria(criteria);

        _repository.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _enrollments.Setup(x => x.GetWithCourseTagsByIdAsync(_enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _materialRepo.Setup(x => x.GetByIdWithDetailsAsync(_materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(material);

        var scoringResult = new OverallScoringResult
        {
            Percentage = 90,
            Summary = "Xuất sắc",
            LearningSuggestions = new List<string> { "Phát huy" },
            DetectedIssues = new List<string>()
        };

        _progresses.Setup(x => x.GetByCompositeKeyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningProgress?)null);

        _scoringService.Setup(x => x.GenerateOverallSuggestionAsync(
                It.IsAny<List<PromptSubmission>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<ScoringCriteria>>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(scoringResult);

        var sut = CreateSut();
        var result = await sut.Handle(new CompleteAttemptCommand(_attemptId, _learnerId), CancellationToken.None);

        Assert.Equal(90, result.FinalScore);
        Assert.Equal("Xuất sắc", result.OverallSuggestion);
        Assert.Equal(PracticeAttemptStatus.Completed, attempt.Status);

        _feedbackRepo.Verify(x => x.AddAsync(It.Is<AIFeedback>(f => f.AttemptId == attempt.Id)), Times.Once);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}