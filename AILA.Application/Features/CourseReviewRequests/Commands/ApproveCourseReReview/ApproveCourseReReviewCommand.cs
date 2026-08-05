using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Commands.ApproveCourseReReview;

/// <summary>
/// Admin phê duyệt yêu cầu — unlock course và publish lại.
/// </summary>
public sealed record ApproveCourseReReviewCommand(
    Guid RequestId,
    string? ReviewComment
) : IRequest<ResponseDto<CourseReviewRequestAdminDto>>;
