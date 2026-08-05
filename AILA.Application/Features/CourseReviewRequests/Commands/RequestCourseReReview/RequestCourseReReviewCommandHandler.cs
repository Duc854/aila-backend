using AILA.Application.Common.Interfaces;
using AILA.Application.Features.CourseReviewRequests.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Commands.RequestCourseReReview;

public sealed class RequestCourseReReviewCommandHandler
    : IRequestHandler<RequestCourseReReviewCommand, ResponseDto<CourseReviewRequestDto>>
{
    private readonly IUnitOfWork _uow;

    public RequestCourseReReviewCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<CourseReviewRequestDto>> Handle(
        RequestCourseReReviewCommand request,
        CancellationToken ct)
    {
        // 1. Validate reason
        if (string.IsNullOrWhiteSpace(request.Reason))
            return ResponseDto<CourseReviewRequestDto>.FailResult(
                "REASON_REQUIRED", "Lý do yêu cầu không được để trống.");

        if (request.Reason.Trim().Length > 1000)
            return ResponseDto<CourseReviewRequestDto>.FailResult(
                "REASON_TOO_LONG", "Lý do không được vượt quá 1000 ký tự.");

        // 2. Lấy course (tracked)
        var course = await _uow.Courses.GetByIdAsync(request.CourseId);
        if (course is null)
            return ResponseDto<CourseReviewRequestDto>.FailResult(
                "COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

        // 3. Chỉ Expert sở hữu mới được gửi
        if (course.ExpertId != request.ExpertId)
            return ResponseDto<CourseReviewRequestDto>.FailResult(
                "FORBIDDEN", "Bạn không có quyền gửi yêu cầu cho khóa học này.");

        // 4. Chỉ gửi khi course đang bị khoá
        if (!course.IsPublicationLocked)
            return ResponseDto<CourseReviewRequestDto>.FailResult(
                "COURSE_NOT_LOCKED", "Khóa học này không đang bị khoá, không cần gửi yêu cầu xem xét.");

        // 5. Chặn gửi trùng khi đang có request Pending
        var hasPending = await _uow.CourseReviewRequests.HasPendingRequestAsync(request.CourseId, ct);
        if (hasPending)
            return ResponseDto<CourseReviewRequestDto>.FailResult(
                "PENDING_EXISTS", "Khóa học này đã có yêu cầu đang chờ xem xét.");

        // 6. Tạo request
        var reviewRequest = new CourseReviewRequest(request.CourseId, request.Reason);
        await _uow.CourseReviewRequests.AddAsync(reviewRequest);
        await _uow.SaveChangesAsync(ct);

        return ResponseDto<CourseReviewRequestDto>.SuccessResult(new CourseReviewRequestDto
        {
            Id             = reviewRequest.Id,
            CourseId       = course.Id,
            CourseName     = course.Name,
            IsCourseLocked = course.IsPublicationLocked,
            Reason         = reviewRequest.Reason,
            Status         = reviewRequest.Status.ToString(),
            ReviewComment  = reviewRequest.ReviewComment,
            CreatedAt      = reviewRequest.CreatedAt,
            ReviewedAt     = reviewRequest.ReviewedAt
        });
    }
}
