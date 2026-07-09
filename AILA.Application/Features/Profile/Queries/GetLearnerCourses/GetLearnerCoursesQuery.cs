using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerCourses
{
    /// <summary>UC-30 "Xem tất cả khóa học đã tham gia" — có phân trang.</summary>
    public record GetLearnerCoursesQuery(Guid UserId, PageRequest Page)
        : IRequest<ResponseDto<PageResult<EnrollmentSummaryDto>>>;
}
