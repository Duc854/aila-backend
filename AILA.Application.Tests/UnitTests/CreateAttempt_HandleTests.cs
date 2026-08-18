using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

public class CreateAttempt_HandleTests
{
    private readonly Mock<IPracticeAttemptRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IAIPracticeMaterialRepository> _materials = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IAccountResourceLimitRepository> _accountResourceLimits = new();
    private readonly Mock<IResourceLimitPolicyRepository> _resourceLimitPolicies = new();
    private readonly Mock<IAccountResourceUsageRepository> _accountResourceUsages = new();
    private readonly Mock<ILearningProgressRepository> _learningProgresses = new();

    private readonly Guid _enrollmentId = Guid.NewGuid();
    private readonly Guid _materialId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();

    public CreateAttempt_HandleTests()
    {
        _unitOfWork.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _unitOfWork.SetupGet(x => x.AIPracticeMaterials).Returns(_materials.Object);
        _unitOfWork.SetupGet(x => x.Subscriptions).Returns(_subscriptions.Object);
        _unitOfWork.SetupGet(x => x.AccountResourceLimits).Returns(_accountResourceLimits.Object);
        _unitOfWork.SetupGet(x => x.ResourceLimitPolicies).Returns(_resourceLimitPolicies.Object);
        _unitOfWork.SetupGet(x => x.AccountResourceUsages).Returns(_accountResourceUsages.Object);
        _unitOfWork.SetupGet(x => x.LearningProgresses).Returns(_learningProgresses.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CreateAttemptCommandHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_EnrollmentNotFound_ThrowsNotFoundException()
    {
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync((Enrollment?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new CreateAttemptCommand(_enrollmentId, _materialId, _learnerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IDOR_DifferentLearner_ThrowsForbiddenAccessException()
    {
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync(enrollment);

        var attackerId = Guid.NewGuid();
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.Handle(new CreateAttemptCommand(_enrollmentId, _materialId, attackerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MaterialNotFound_ThrowsNotFoundException()
    {
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync(enrollment);
        _materials.Setup(x => x.GetByIdAsync(_materialId)).ReturnsAsync((AIPracticeMaterial?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new CreateAttemptCommand(_enrollmentId, _materialId, _learnerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_QuotaExceeded_ThrowsInvalidOperationException()
    {
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);
        var material = new AIPracticeMaterial(_materialId, "Scenario", "AITask", "LearnerTask", PracticeDifficulty.Easy, 3);

        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync(enrollment);
        _materials.Setup(x => x.GetByIdAsync(_materialId)).ReturnsAsync(material);

        var usage = new AccountResourceUsage(_learnerId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29));
        usage.ConsumeAiPracticeScenario(5);

        _accountResourceUsages.Setup(x => x.GetByAccountIdAsync(_learnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usage);

        var policy = new ResourceLimitPolicy(ResourceAccountType.Learner, 50000, 5, 5);
        _resourceLimitPolicies.Setup(x => x.GetByAccountTypeAsync(ResourceAccountType.Learner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(new CreateAttemptCommand(_enrollmentId, _materialId, _learnerId), CancellationToken.None));

        Assert.Contains("hết định ngạch", ex.Message);
    }

    [Fact]
    public async Task Handle_ValidScenario_CreatesAttemptAndConsumesQuota()
    {
        var enrollment = new Enrollment(_learnerId, Guid.NewGuid(), 3);
        var material = new AIPracticeMaterial(_materialId, "Scenario", "AITask", "LearnerTask", PracticeDifficulty.Easy, 3);

        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync(enrollment);
        _materials.Setup(x => x.GetByIdAsync(_materialId)).ReturnsAsync(material);

        var usage = new AccountResourceUsage(_learnerId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29));
        _accountResourceUsages.Setup(x => x.GetByAccountIdAsync(_learnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usage);

        var policy = new ResourceLimitPolicy(ResourceAccountType.Learner, 50000, 10, 5);
        _resourceLimitPolicies.Setup(x => x.GetByAccountTypeAsync(ResourceAccountType.Learner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var result = await sut.Handle(new CreateAttemptCommand(_enrollmentId, _materialId, _learnerId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(1, usage.AiPracticeScenarioUsed);
        _repository.Verify(x => x.AddAsync(It.IsAny<PracticeAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}