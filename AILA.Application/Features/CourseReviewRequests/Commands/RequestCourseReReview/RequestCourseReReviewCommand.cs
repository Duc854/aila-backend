using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Commands.RequestCourseReReview;

/// <summary>
/// Expert gửi yêu cầu mở lại khóa học đang bị khoá (IsPublicationLocked = true).
/// </summary>
public sealed record RequestCourseReReviewCommand(
    Guid CourseId,
    Guid ExpertId,
    string Reason
) : IRequest<ResponseDto<CourseReviewRequestDto>>;
