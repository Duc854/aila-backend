using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using AILA.Application.Features.Profile.Queries;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerAiScenarios
{
    public class GetLearnerAiScenariosQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetLearnerAiScenariosQuery, ResponseDto<PageResult<AiScenarioHistoryItemDto>>>
    {
        public async Task<ResponseDto<PageResult<AiScenarioHistoryItemDto>>> Handle(
            GetLearnerAiScenariosQuery request, CancellationToken ct)
        {
            var (pageIndex, pageSize) = PagingDefaults.Normalize(request.Page);

            // Ownership (BR-01): lọc theo learner đang đăng nhập; đọc nguyên trạng dữ liệu đã lưu (BR-02).
            var (items, total) = await uow.PracticeAttempts.GetPagedCompletedScenarioHistoryByLearnerAsync(
                request.UserId, pageIndex, pageSize, ct);

            var page = new PageResult<AiScenarioHistoryItemDto>(items, total, pageIndex, pageSize);

            return ResponseDto<PageResult<AiScenarioHistoryItemDto>>.SuccessResult(page);
        }
    }
}
