using AILA.Application.Common.Interfaces;
using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Commands.RejectCourseReReview;

public sealed class RejectCourseReReviewCommandHandler
    : IRequestHandler<RejectCourseReReviewCommand, ResponseDto<CourseReviewRequestAdminDto>>
{
    private readonly IUnitOfWork _uow;

    public RejectCourseReReviewCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<CourseReviewRequestAdminDto>> Handle(
        RejectCourseReReviewCommand request,
        CancellationToken ct)
    {
        // 1. Validate review comment
        if (string.IsNullOrWhiteSpace(request.ReviewComment))
            return ResponseDto<CourseReviewRequestAdminDto>.FailResult(
                "COMMENT_REQUIRED", "Lý do từ chối không được để trống.");

        // 2. Load request kèm Course (tracked)
        var reviewRequest = await _uow.CourseReviewRequests.GetWithCourseAsync(request.RequestId, ct);
        if (reviewRequest is null)
            return ResponseDto<CourseReviewRequestAdminDto>.FailResult(
                "REQUEST_NOT_FOUND", "Không tìm thấy yêu cầu xem xét.");

        // 3. Domain: reject request — course giữ nguyên trạng thái bị khoá
        try
        {
            reviewRequest.Reject(request.ReviewComment);
        }
        catch (InvalidOperationException ex)
        {
            return ResponseDto<CourseReviewRequestAdminDto>.FailResult("ALREADY_PROCESSED", ex.Message);
        }

        await _uow.SaveChangesAsync(ct);

        var course = reviewRequest.Course;
        var expert = course.Expert;

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
