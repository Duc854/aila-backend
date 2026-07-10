using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using AILA.Application.Features.Profile.Mappers;
using AILA.Application.Features.Profile.Queries;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerQuizHistory
{
    public class GetLearnerQuizHistoryQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetLearnerQuizHistoryQuery, ResponseDto<PageResult<QuizHistoryItemDto>>>
    {
        public async Task<ResponseDto<PageResult<QuizHistoryItemDto>>> Handle(
            GetLearnerQuizHistoryQuery request, CancellationToken ct)
        {
            var (pageIndex, pageSize) = PagingDefaults.Normalize(request.Page);

            // Ownership (BR-01): lọc theo learner đang đăng nhập; đọc nguyên trạng dữ liệu đã lưu (BR-02).
            var (items, total) = await uow.Quizzes.GetPagedSubmittedAttemptsByLearnerAsync(
                request.UserId, pageIndex, pageSize, ct);

            var page = new PageResult<QuizHistoryItemDto>(
                items.Select(a => a.ToHistoryDto()), total, pageIndex, pageSize);

            return ResponseDto<PageResult<QuizHistoryItemDto>>.SuccessResult(page);
        }
    }
}
