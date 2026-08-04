using AILA.Application.Common.Interfaces;
using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Queries.GetCourseReReviewRequests;

public sealed class GetCourseReReviewRequestsQueryHandler
    : IRequestHandler<GetCourseReReviewRequestsQuery, ResponseDto<List<CourseReviewRequestAdminDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetCourseReReviewRequestsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<List<CourseReviewRequestAdminDto>>> Handle(
        GetCourseReReviewRequestsQuery request,
        CancellationToken ct)
    {
        var items = await _uow.CourseReviewRequests
            .GetAllWithDetailsAsync(request.Status, ct);

        var result = items.Select(r => new CourseReviewRequestAdminDto
        {
            Id             = r.Id,
            CourseId       = r.CourseId,
            CourseName     = r.Course?.Name ?? string.Empty,
            IsCourseLocked = r.Course?.IsPublicationLocked ?? false,
            ExpertId       = r.Course?.Expert?.UserId ?? Guid.Empty,
            ExpertName     = r.Course?.Expert?.User?.FullName ?? string.Empty,
            ExpertEmail    = r.Course?.Expert?.User?.Email ?? string.Empty,
            Reason         = r.Reason,
            Status         = r.Status.ToString(),
            ReviewComment  = r.ReviewComment,
            CreatedAt      = r.CreatedAt,
            ReviewedAt     = r.ReviewedAt
        }).ToList();

        return ResponseDto<List<CourseReviewRequestAdminDto>>.SuccessResult(result);
    }
}
