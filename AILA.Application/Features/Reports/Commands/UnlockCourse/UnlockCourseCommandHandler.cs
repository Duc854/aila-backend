using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.UnlockCourse;

public sealed class UnlockCourseCommandHandler
    : IRequestHandler<UnlockCourseCommand, ResponseDto<CourseModerationResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public UnlockCourseCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<CourseModerationResponseDto>> Handle(
        UnlockCourseCommand request,
        CancellationToken ct)
    {
        // 1. Load course (tracked để EF lưu thay đổi)
        var course = await _uow.Courses.GetByIdAsync(request.CourseId);

        if (course is null)
            return ResponseDto<CourseModerationResponseDto>.FailResult(
                "COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

        // 2. Chỉ unlock khi đang bị lock
        if (!course.IsPublicationLocked)
            return ResponseDto<CourseModerationResponseDto>.FailResult(
                "NOT_LOCKED", "Khóa học này không đang bị khoá.");

        // 3. Domain action
        course.RestorePublication();

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<CourseModerationResponseDto>.SuccessResult(
            new CourseModerationResponseDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                IsPublished = course.IsPublished,
                IsPublicationLocked = course.IsPublicationLocked,
                Message = "Khóa học đã được phục hồi."
            });
    }
}
