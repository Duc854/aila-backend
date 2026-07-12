using AILA.Application.Features.Profile.Dtos;
using AILA.Application.Features.Profile.Queries;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerAiScenarios
{
    public class GetLearnerAiScenariosQueryHandler
        : IRequestHandler<GetLearnerAiScenariosQuery, ResponseDto<PageResult<AiScenarioHistoryItemDto>>>
    {
        public Task<ResponseDto<PageResult<AiScenarioHistoryItemDto>>> Handle(
            GetLearnerAiScenariosQuery request, CancellationToken ct)
        {
            var (pageIndex, pageSize) = PagingDefaults.Normalize(request.Page);

            // AC-5: luồng AI practice chưa lưu bản ghi nào ở tầng domain → trả về trang rỗng (không lỗi).
            // Khi có entity/luồng AI-practice-record, thay bằng truy vấn phân trang thật lọc theo learnerId (BR-01).
            var page = new PageResult<AiScenarioHistoryItemDto>(
                Enumerable.Empty<AiScenarioHistoryItemDto>(), totalItems: 0, pageIndex, pageSize);

            return Task.FromResult(ResponseDto<PageResult<AiScenarioHistoryItemDto>>.SuccessResult(page));
        }
    }
}
