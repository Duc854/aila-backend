using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Courses.Commands;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT19_EnrollCourse — <see cref="EnrollCourseCommandHandler.Handle"/>
/// Module: Course · CC = 5 · 6 test case
///
/// Nhánh: B1 = course null · B2 = !course.IsPublished · B3 = learner null
///        B4 = đã tham gia khóa học này rồi
///
/// Mọi nhánh lỗi đều ném InvalidOperationException (không trả ResponseDto).
/// Bất biến: TotalMaterials của Enrollment được chốt tại thời điểm ghi danh, lấy từ
/// CountMaterialsAsync — đây là mẫu số dùng để tính ProgressPct suốt vòng đời (xem UT12/UT23).
///
/// LƯU Ý THAY ĐỔI: handler nay nhận thêm <see cref="ILearnerBehaviorService"/>, đọc khóa học
/// qua GetWithTagsAsync (không phải GetByIdAsync) và chốt dữ liệu bằng
/// BeginTransaction/CommitTransaction thay cho SaveChangesAsync.
/// </summary>
public class UT19_EnrollCourse_HandleTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LearnerId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IGenericRepository<Learner>> _learners = new();
    private readonly Mock<ILearnerBehaviorService> _behavior = new();

    public UT19_EnrollCourse_HandleTests()
    {
        _uow.SetupGet(x => x.Courses).Returns(_courses.Object);
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.Setup(x => x.Repository<Learner>()).Returns(_learners.Object);

        _learners.Setup(x => x.GetByIdAsync(LearnerId)).ReturnsAsync(new Learner(LearnerId));
        _enrollments.Setup(x => x.GetByLearnerAndCourseAsync(LearnerId, CourseId))
                    .ReturnsAsync((Enrollment?)null);
        _courses.Setup(x => x.CountMaterialsAsync(CourseId)).ReturnsAsync(5);
    }

    private EnrollCourseCommandHandler CreateSut() => new(_uow.Object, _behavior.Object);

    private static Course BuildCourse(bool published)
    {
        var course = new Course("Prompt Engineering 101", Guid.NewGuid(), Guid.NewGuid(), KnowledgeLevel.Beginner);
        if (published)
        {
            var module = new Module(course.Id, "Học phần mở đầu", 1);
            module.AddMaterial(Material.CreateVideo(module.Id, "Bài học đầu tiên", 1));
            course.AddModule(module);
            course.Publish();
        }
        return course;
    }

    private void SetupCourse(Course? course) =>
        _courses.Setup(x => x.GetWithTagsAsync(CourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

    private Task<EnrollmentResultDto> Act() =>
        CreateSut().Handle(new EnrollCourseCommand(CourseId, LearnerId), CancellationToken.None);

    /// <summary>
    /// Các nhánh lỗi đều bật TRƯỚC khi mở transaction ⇒ không được ghi enrollment,
    /// cũng không được đụng tới transaction (kể cả rollback).
    /// </summary>
    private void AssertNotPersisted()
    {
        _enrollments.Verify(x => x.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID01 · B1=T · Type A — khóa học không tồn tại.</summary>
    [Fact]
    public async Task UTCID01_CourseNotFound_Throws()
    {
        SetupCourse(null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Act);

        Assert.Equal("Khóa học không tồn tại.", ex.Message);
        AssertNotPersisted();
    }

    /// <summary>UTCID02 · B1=F, B2=T · Type A — khóa học còn ở dạng bản nháp.</summary>
    [Fact]
    public async Task UTCID02_CourseNotPublished_Throws()
    {
        SetupCourse(BuildCourse(published: false));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Act);

        Assert.Equal("Khóa học chưa được công khai.", ex.Message);
        AssertNotPersisted();
    }

    /// <summary>UTCID03 · B3=T · Type A — hồ sơ học viên không tồn tại.</summary>
    [Fact]
    public async Task UTCID03_LearnerNotFound_Throws()
    {
        SetupCourse(BuildCourse(published: true));
        _learners.Setup(x => x.GetByIdAsync(LearnerId)).ReturnsAsync((Learner?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Act);

        Assert.Equal("Học viên không tồn tại.", ex.Message);
        AssertNotPersisted();
    }

    /// <summary>UTCID04 · B4=T · Type A — đã ghi danh trước đó (chống ghi danh trùng).</summary>
    [Fact]
    public async Task UTCID04_AlreadyEnrolled_Throws()
    {
        SetupCourse(BuildCourse(published: true));
        _enrollments.Setup(x => x.GetByLearnerAndCourseAsync(LearnerId, CourseId))
                    .ReturnsAsync(new Enrollment(LearnerId, CourseId, 5));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Act);

        Assert.Equal("Bạn đã tham gia khóa học này rồi.", ex.Message);
        AssertNotPersisted();
    }

    /// <summary>UTCID05 · Toàn bộ nhánh = F · Type N — ghi danh thành công.</summary>
    [Fact]
    public async Task UTCID05_HappyPath_CreatesActiveEnrollmentWithMaterialCount()
    {
        SetupCourse(BuildCourse(published: true));

        var result = await Act();

        Assert.Equal(CourseId, result.CourseId);
        Assert.Equal(LearnerId, result.LearnerId);
        Assert.Equal(nameof(EnrollmentStatus.Active), result.Status);
        Assert.NotEqual(Guid.Empty, result.EnrollmentId);

        _enrollments.Verify(x => x.AddAsync(It.Is<Enrollment>(e =>
            e.TotalMaterials == 5 && e.CompletedMaterials == 0 && e.ProgressPct == 0.00m)), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID06 · Type B — khóa học chưa có học liệu nào (biên dưới TotalMaterials = 0).</summary>
    [Fact]
    public async Task UTCID06_CourseWithZeroMaterials_StillEnrolls()
    {
        SetupCourse(BuildCourse(published: true));
        _courses.Setup(x => x.CountMaterialsAsync(CourseId)).ReturnsAsync(0);

        var result = await Act();

        Assert.Equal(nameof(EnrollmentStatus.Active), result.Status);
        _enrollments.Verify(x => x.AddAsync(It.Is<Enrollment>(e =>
            e.TotalMaterials == 0 && e.ProgressPct == 0.00m)), Times.Once);
    }

    /// <summary>
    /// UTCID07 · Type A — khóa học KHÔNG có interest tag nào ⇒ bỏ qua bước cộng điểm hành vi.
    /// Course dựng từ constructor chưa gắn tag nên nhánh <c>behaviorTags.Any() == false</c>
    /// là nhánh mặc định của mọi case ở trên; case này khẳng định rõ điều đó.
    /// </summary>
    [Fact]
    public async Task UTCID07_CourseWithoutInterestTags_SkipsBehaviorScoring()
    {
        SetupCourse(BuildCourse(published: true));

        await Act();

        _behavior.Verify(x => x.IncreaseScoreAsync(
            It.IsAny<Guid>(), It.IsAny<IEnumerable<Tag>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// UTCID08 · Type A — lỗi giữa transaction ⇒ phải RollbackTransaction và ném tiếp exception.
    /// Phủ khối catch mà các case trên không chạm tới.
    /// </summary>
    [Fact]
    public async Task UTCID08_FailureInsideTransaction_RollsBackAndRethrows()
    {
        SetupCourse(BuildCourse(published: true));
        _enrollments.Setup(x => x.AddAsync(It.IsAny<Enrollment>()))
                    .ThrowsAsync(new InvalidOperationException("Lỗi ghi dữ liệu."));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Act);

        Assert.Equal("Lỗi ghi dữ liệu.", ex.Message);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
