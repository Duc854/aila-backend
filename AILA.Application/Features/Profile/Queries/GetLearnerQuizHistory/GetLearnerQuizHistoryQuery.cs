using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerQuizHistory
{
    /// <summary>UC-30 "Xem tất cả lịch sử quiz đã làm" — có phân trang.</summary>
    public record GetLearnerQuizHistoryQuery(Guid UserId, PageRequest Page)
        : IRequest<ResponseDto<PageResult<QuizHistoryItemDto>>>;
}
