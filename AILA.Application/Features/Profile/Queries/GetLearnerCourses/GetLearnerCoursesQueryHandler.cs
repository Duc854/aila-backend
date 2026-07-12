using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using AILA.Application.Features.Profile.Mappers;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerCourses
{
    public class GetLearnerCoursesQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetLearnerCoursesQuery, ResponseDto<PageResult<EnrollmentSummaryDto>>>
    {
        public async Task<ResponseDto<PageResult<EnrollmentSummaryDto>>> Handle(
            GetLearnerCoursesQuery request, CancellationToken ct)
        {
            var (pageIndex, pageSize) = PagingDefaults.Normalize(request.Page);

            // Ownership (BR-01): lọc theo learner đang đăng nhập.
            var (items, total) = await uow.Enrollments.GetPagedEnrollmentsByLearnerAsync(
                request.UserId, pageIndex, pageSize, ct);

            var page = new PageResult<EnrollmentSummaryDto>(
                items.Select(e => e.ToSummaryDto()), total, pageIndex, pageSize);

            return ResponseDto<PageResult<EnrollmentSummaryDto>>.SuccessResult(page);
        }
    }
}
