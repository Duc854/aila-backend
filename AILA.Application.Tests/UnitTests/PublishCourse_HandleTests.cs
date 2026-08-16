using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Courses.Commands;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT17_PublishCourseHandler — <see cref="PublishCourseCommandHandler.Handle"/>
/// Module: Course · CC = 4 · 5 test case
///
/// Nhánh: B1 = course null · B2 = ExpertId không khớp (phân quyền sở hữu)
///        B3 = catch InvalidOperationException do domain Course.Publish() ném
///
/// B2 là hàng rào phân quyền: chỉ Expert sở hữu mới được xuất bản khóa học của mình.
/// B3 cho thấy handler KHÔNG tự validate mà uỷ quyền hoàn toàn cho domain (xem UT16),
/// chỉ dịch exception thành mã lỗi nghiệp vụ.
/// </summary>
public class UT17_PublishCourse_HandleTests
{
    private static readonly Guid ExpertId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();

    public UT17_PublishCourse_HandleTests()
    {
        _uow.SetupGet(x => x.Courses).Returns(_courses.Object);
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        
        // Default setup for enrollments
        _enrollments.Setup(x => x.GetByCourseIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<Enrollment>());
        _courses.Setup(x => x.CountMaterialsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(5);
    }

    private PublishCourseCommandHandler CreateSut() => new(_uow.Object);

    private static Course BuildCourse(Guid expertId, bool withValidModule)
    {
        var course = new Course("Prompt Engineering 101", Guid.NewGuid(), expertId, KnowledgeLevel.Beginner);
        if (withValidModule)
        {
            var module = new Module(course.Id, "Học phần mở đầu", 1);
            module.AddMaterial(Material.CreateVideo(module.Id, "Bài học đầu tiên", 1));
            course.AddModule(module);
        }
        return course;
    }

    private Course SetupCourse(Course? course)
    {
        _courses.Setup(x => x.GetWithTagsForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);
        return course!;
    }

    private Task<Shared.Wrappers.ResponseDto<object>> Act(Guid? expertId = null) =>
        CreateSut().Handle(new PublishCourseCommand(Guid.NewGuid(), expertId ?? ExpertId), CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — khóa học không tồn tại.</summary>
    [Fact]
    public async Task UTCID01_CourseNotFound_ReturnsCourseNotFound()
    {
        SetupCourse(null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("COURSE_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B2=T · Type A — Expert khác cố xuất bản khóa học không phải của mình.</summary>
    [Fact]
    public async Task UTCID02_NotOwner_ReturnsForbiddenWithoutPublishing()
    {
        var course = SetupCourse(BuildCourse(ExpertId, withValidModule: true));

        var result = await Act(expertId: Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.False(course.IsPublished);
        _courses.Verify(x => x.Update(It.IsAny<Course>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID03 · B3=T · Type A — domain từ chối vì khóa học chưa có học phần nào.</summary>
    [Fact]
    public async Task UTCID03_DomainRuleViolated_ReturnsPublishFailedWithDomainMessage()
    {
        var course = SetupCourse(BuildCourse(ExpertId, withValidModule: false));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("PUBLISH_FAILED", result.ErrorCode);
        Assert.Equal("Khóa học phải có ít nhất một học phần trước khi xuất bản.", result.ErrorMessage);
        Assert.False(course.IsPublished);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · Toàn bộ nhánh = F · Type N — xuất bản thành công.</summary>
    [Fact]
    public async Task UTCID04_HappyPath_PublishesAndSaves()
    {
        var course = SetupCourse(BuildCourse(ExpertId, withValidModule: true));

        var result = await Act();

        Assert.True(result.Success);
        Assert.True(course.IsPublished);
        _courses.Verify(x => x.Update(course), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID05 · B3=F · Type B — khóa học đã publish sẵn (idempotent).
    /// Course.Publish() return im lặng ⇒ handler vẫn trả Success và vẫn lưu.
    /// </summary>
    [Fact]
    public async Task UTCID05_AlreadyPublished_IsIdempotent()
    {
        var course = BuildCourse(ExpertId, withValidModule: true);
        course.Publish();
        SetupCourse(course);

        var result = await Act();

        Assert.True(result.Success);
        Assert.True(course.IsPublished);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID06 · Enrollment UpdateTotalMaterials functionality test.
    /// Verify that when a course is published, all enrollments get updated with current material count.
    /// </summary>
    [Fact]
    public async Task UTCID06_PublishWithEnrollments_UpdatesEnrollmentTotalMaterials()
    {
        // Arrange
        var course = BuildCourse(ExpertId, withValidModule: true);
        var courseId = course.Id;
        SetupCourse(course);
        
        var learner1Id = Guid.NewGuid();
        var learner2Id = Guid.NewGuid();
        
        var enrollment1 = new Enrollment(learner1Id, courseId, 3); // Old count: 3 materials
        var enrollment2 = new Enrollment(learner2Id, courseId, 3); // Old count: 3 materials
        var enrollments = new List<Enrollment> { enrollment1, enrollment2 };
        
        // Setup specific mocks for this test
        _enrollments.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(enrollments);
        _courses.Setup(x => x.CountMaterialsAsync(courseId))
                .ReturnsAsync(5); // New count: 5 materials

        // Act
        var result = await CreateSut().Handle(new PublishCourseCommand(courseId, ExpertId), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(course.IsPublished);
        
        // Verify enrollments were updated
        Assert.Equal(5, enrollment1.TotalMaterials);
        Assert.Equal(5, enrollment2.TotalMaterials);
        
        // Verify Update was called for each enrollment
        _enrollments.Verify(x => x.Update(enrollment1), Times.Once);
        _enrollments.Verify(x => x.Update(enrollment2), Times.Once);
        
        // Verify response includes enrollment count - use reflection since it's an anonymous type
        var responseData = result.Data;
        var enrollmentsUpdatedProp = responseData?.GetType().GetProperty("EnrollmentsUpdated");
        var enrollmentsUpdated = enrollmentsUpdatedProp?.GetValue(responseData);
        Assert.Equal(2, enrollmentsUpdated);
    }
}
