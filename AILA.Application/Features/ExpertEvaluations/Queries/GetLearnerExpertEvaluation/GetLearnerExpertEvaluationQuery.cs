using AILA.Application.Features.ExpertEvaluations.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Queries.GetLearnerExpertEvaluation
{
    /// <summary>
    /// UC-30: học viên xem lại yêu cầu của mình cùng kết quả AI và kết quả chuyên gia.
    /// </summary>
    public record GetLearnerExpertEvaluationQuery(
        Guid RequestId,
        Guid LearnerId) : IRequest<ResponseDto<LearnerExpertEvaluationDto>>;
}
