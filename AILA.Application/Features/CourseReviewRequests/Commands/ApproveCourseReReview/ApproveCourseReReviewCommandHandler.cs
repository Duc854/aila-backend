using AILA.Application.Common.Interfaces;
using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Commands.ApproveCourseReReview;

public sealed class ApproveCourseReReviewCommandHandler
    : IRequestHandler<ApproveCourseReReviewCommand, ResponseDto<CourseReviewRequestAdminDto>>
{
    private readonly IUnitOfWork _uow;

    public ApproveCourseReReviewCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<CourseReviewRequestAdminDto>> Handle(
        ApproveCourseReReviewCommand request,
        CancellationToken ct)
    {
        // 1. Load request kèm Course (tracked)
        var reviewRequest = await _uow.CourseReviewRequests.GetWithCourseAsync(request.RequestId, ct);
        if (reviewRequest is null)
            return ResponseDto<CourseReviewRequestAdminDto>.FailResult(
                "REQUEST_NOT_FOUND", "Không tìm thấy yêu cầu xem xét.");

        // 2. Domain: approve request
        try
        {
            reviewRequest.Approve(request.ReviewComment);
        }
        catch (InvalidOperationException ex)
        {
            return ResponseDto<CourseReviewRequestAdminDto>.FailResult("ALREADY_PROCESSED", ex.Message);
        }

        // 3. Domain: unlock + publish course
        try
        {
            reviewRequest.Course.RestorePublication();
        }
        catch (InvalidOperationException ex)
        {
            return ResponseDto<CourseReviewRequestAdminDto>.FailResult("RESTORE_FAILED", ex.Message);
        }

        await _uow.SaveChangesAsync(ct);

        var course  = reviewRequest.Course;
        var expert  = course.Expert;

        return ResponseDto<CourseReviewRequestAdminDto>.SuccessResult(new CourseReviewRequestAdminDto
        {
            Id             = reviewRequest.Id,
            CourseId       = course.Id,
            CourseName     = course.Name,
            IsCourseLocked = course.IsPublicationLocked,
            ExpertId       = expert?.UserId ?? Guid.Empty,
            ExpertName     = expert?.User?.FullName ?? string.Empty,
            ExpertEmail    = expert?.User?.Email ?? string.Empty,
            Reason         = reviewRequest.Reason,
            Status         = reviewRequest.Status.ToString(),
            ReviewComment  = reviewRequest.ReviewComment,
            CreatedAt      = reviewRequest.CreatedAt,
            ReviewedAt     = reviewRequest.ReviewedAt
        });
    }
}
