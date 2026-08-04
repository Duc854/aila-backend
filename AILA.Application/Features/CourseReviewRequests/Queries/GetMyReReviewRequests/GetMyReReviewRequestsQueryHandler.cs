using AILA.Application.Common.Interfaces;
using AILA.Application.Features.CourseReviewRequests.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.CourseReviewRequests.Queries.GetMyReReviewRequests;

public sealed class GetMyReReviewRequestsQueryHandler
    : IRequestHandler<GetMyReReviewRequestsQuery, ResponseDto<List<CourseReviewRequestDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetMyReReviewRequestsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<List<CourseReviewRequestDto>>> Handle(
        GetMyReReviewRequestsQuery request,
        CancellationToken ct)
    {
        var items = await _uow.CourseReviewRequests
            .GetByExpertAsync(request.ExpertId, ct);

        var result = items.Select(r => new CourseReviewRequestDto
        {
            Id             = r.Id,
            CourseId       = r.CourseId,
            CourseName     = r.Course?.Name ?? string.Empty,
            IsCourseLocked = r.Course?.IsPublicationLocked ?? false,
            Reason         = r.Reason,
            Status         = r.Status.ToString(),
            ReviewComment  = r.ReviewComment,
            CreatedAt      = r.CreatedAt,
            ReviewedAt     = r.ReviewedAt
        }).ToList();

        return ResponseDto<List<CourseReviewRequestDto>>.SuccessResult(result);
    }
}
