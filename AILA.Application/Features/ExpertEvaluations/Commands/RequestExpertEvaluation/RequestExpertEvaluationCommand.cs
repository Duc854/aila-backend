using AILA.Application.Features.ExpertEvaluations.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Commands.RequestExpertEvaluation
{
    /// <summary>
    /// UC-29: học viên tiêu một lượt quota để nhờ chuyên gia đánh giá một lượt thực hành.
    /// <paramref name="LearnerId"/> luôn lấy từ token, không nhận từ payload.
    /// </summary>
    public record RequestExpertEvaluationCommand(
        Guid PracticeAttemptId,
        Guid LearnerId) : IRequest<ResponseDto<ExpertEvaluationRequestCreatedDto>>;
}
