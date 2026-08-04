using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Commands.RejectCourseReReview;

/// <summary>
/// Admin từ chối yêu cầu — course vẫn bị khoá.
/// </summary>
public sealed record RejectCourseReReviewCommand(
    Guid RequestId,
    string ReviewComment
) : IRequest<ResponseDto<CourseReviewRequestAdminDto>>;
