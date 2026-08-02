using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Queries.GetMyReReviewRequests;

/// <summary>
/// Expert lấy danh sách các yêu cầu xem xét lại do mình gửi.
/// </summary>
public sealed record GetMyReReviewRequestsQuery(
    Guid ExpertId
) : IRequest<ResponseDto<List<CourseReviewRequestDto>>>;
