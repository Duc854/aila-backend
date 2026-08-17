using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Materials.Commands.MarkMaterialAsCompleted;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT22_MarkMaterialCompleted — <see cref="MarkMaterialAsCompletedCommandHandler.Handle"/>
/// Module: Learning · CC = 6 · 7 test case
///
/// Nhánh: B1 = enrollment null · B2 = học liệu không thuộc khóa học
///        B3 = progress chưa tồn tại (tạo mới) · B4 = progress đã hoàn thành (idempotent)
///        B5 = học viên vừa hoàn thành TOÀN BỘ khóa ⇒ cộng điểm hành vi
///        B6 = catch ⇒ rollback + ném lại
///
/// Điểm mấu chốt: nhánh idempotent (B4) gọi RollbackTransactionAsync chứ KHÔNG Commit,
/// nhưng vẫn trả Success ⇒ chỉ phân biệt được bằng Verify trên mock.
///
/// LƯU Ý THAY ĐỔI: handler nay nhận thêm <see cref="ILearnerBehaviorService"/>, đọc enrollment
/// qua GetWithCourseTagsAsync(learnerId, courseId) — CHÚ Ý THỨ TỰ THAM SỐ ngược với
/// GetByCourseAndLearnerAsync của bản cũ — và không còn gọi Enrollments.Update()
/// hay SaveChangesAsync(); dữ liệu được chốt bằng CommitTransactionAsync().
/// </summary>
public class UT22_MarkMaterialAsCompleted_HandleTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid MaterialId = Guid.NewGuid();
    private static readonly Guid LearnerId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<ILearningProgressRepository> _progresses = new();
    private readonly Mock<ILearnerBehaviorService> _behavior = new();

    private readonly Enrollment _enrollment = new(LearnerId, CourseId, totalMaterials: 3);

    public UT22_MarkMaterialAsCompleted_HandleTests()
    {
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.SetupGet(x => x.Materials).Returns(_materials.Object);
        _uow.SetupGet(x => x.LearningProgresses).Returns(_progresses.Object);

        _enrollments.Setup(x => x.GetWithCourseTagsAsync(LearnerId, CourseId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_enrollment);
        _materials.Setup(x => x.IsMaterialInCourseAsync(MaterialId, CourseId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        _progresses.Setup(x => x.GetByCompositeKeyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((LearningProgress?)null);
    }

    private MarkMaterialAsCompletedCommandHandler CreateSut() => new(_uow.Object, _behavior.Object);

    private Task<Shared.Wrappers.ResponseDto<bool>> Act() =>
        CreateSut().Handle(new MarkMaterialAsCompletedCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — chưa ghi danh khóa học.</summary>
    [Fact]
    public async Task UTCID01_EnrollmentNotFound_ReturnsEnrollmentNotFound()
    {
        _enrollments.Setup(x => x.GetWithCourseTagsAsync(LearnerId, CourseId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("ENROLLMENT_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B1=F, B2=T · Type A — học liệu không thuộc khóa học này.</summary>
    [Fact]
    public async Task UTCID02_MaterialNotInCourse_ReturnsMaterialNotFound()
    {
        _materials.Setup(x => x.IsMaterialInCourseAsync(MaterialId, CourseId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("MATERIAL_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID03 · B3=T · Type N — lần đầu học bài này: tạo LearningProgress mới.</summary>
    [Fact]
    public async Task UTCID03_FirstTimeCompletion_CreatesProgressAndCommits()
    {
        var result = await Act();

        Assert.True(result.Success);
        Assert.True(result.Data);
        _progresses.Verify(x => x.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, _enrollment.CompletedMaterials);
        Assert.Equal(33.33m, _enrollment.ProgressPct);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · B3=F, B4=F · Type N — đã mở bài nhưng chưa hoàn thành.</summary>
    [Fact]
    public async Task UTCID04_ExistingIncompleteProgress_CompletesAndCommits()
    {
        var progress = new LearningProgress(_enrollment.Id, MaterialId);
        _progresses.Setup(x => x.GetByCompositeKeyAsync(_enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(progress);

        var result = await Act();

        Assert.True(result.Success);
        Assert.True(progress.IsCompleted);
        _progresses.Verify(x => x.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(1, _enrollment.CompletedMaterials);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID05 · B4=T · Type A — bài đã hoàn thành từ trước (idempotent).
    /// Trả Success nhưng ROLLBACK, KHÔNG commit và KHÔNG cộng tiến độ lần hai.
    /// </summary>
    [Fact]
    public async Task UTCID05_AlreadyCompleted_RollsBackWithoutIncrementingProgress()
    {
        var progress = new LearningProgress(_enrollment.Id, MaterialId);
        progress.Complete();
        _progresses.Setup(x => x.GetByCompositeKeyAsync(_enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(progress);

        var result = await Act();

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal(0, _enrollment.CompletedMaterials);
        Assert.Equal(0.00m, _enrollment.ProgressPct);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID06 · B5=T · Type B — hoàn thành học liệu CUỐI CÙNG ⇒ enrollment chuyển sang
    /// Completed và hệ thống cộng điểm hành vi theo tag của khóa học.
    /// Enrollment phải được nạp sẵn navigation Course (đúng như GetWithCourseTagsAsync làm),
    /// nếu không handler sẽ ném NullReferenceException tại enrollment.Course.CourseTags.
    /// </summary>
    [Fact]
    public async Task UTCID06_CompletingLastMaterial_IncreasesBehaviorScore()
    {
        var enrollment = new Enrollment(LearnerId, CourseId, totalMaterials: 1);
        PrivateSetter.Set(enrollment, nameof(Enrollment.Course),
            new Course("Prompt Engineering 101", Guid.NewGuid(), Guid.NewGuid(), KnowledgeLevel.Beginner));
        _enrollments.Setup(x => x.GetWithCourseTagsAsync(LearnerId, CourseId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(enrollment);

        var result = await Act();

        Assert.True(result.Success);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.Equal(100.00m, enrollment.ProgressPct);
        _behavior.Verify(x => x.IncreaseScoreAsync(
            LearnerId, It.IsAny<IEnumerable<Tag>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID07 · B6=T · Type A — lưu thất bại ⇒ rollback và ném lại exception.</summary>
    [Fact]
    public async Task UTCID07_CommitFails_RollsBackAndRethrows()
    {
        _uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Act);

        Assert.Equal("db down", ex.Message);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
