using AILA.Application.Features.CourseReviewRequests.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Queries.GetCourseReReviewRequests;

/// <summary>
/// Admin lấy danh sách tất cả review requests, có thể filter theo status.
/// </summary>
public sealed record GetCourseReReviewRequestsQuery(
    CourseReviewRequestStatus? Status
) : IRequest<ResponseDto<List<CourseReviewRequestAdminDto>>>;
