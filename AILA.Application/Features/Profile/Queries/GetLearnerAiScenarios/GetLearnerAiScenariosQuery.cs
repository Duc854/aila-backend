using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerAiScenarios
{
    /// <summary>UC-30 "Xem tất cả lịch sử AI scenario đã làm" — có phân trang.</summary>
    public record GetLearnerAiScenariosQuery(Guid UserId, PageRequest Page)
        : IRequest<ResponseDto<PageResult<AiScenarioHistoryItemDto>>>;
}
