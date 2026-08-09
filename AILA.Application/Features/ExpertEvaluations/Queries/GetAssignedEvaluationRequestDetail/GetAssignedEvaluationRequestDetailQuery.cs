using AILA.Application.Features.ExpertEvaluations.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Queries.GetAssignedEvaluationRequestDetail
{
    /// <summary>
    /// UC-63 (detail): chuyên gia mở một yêu cầu được giao để xem đủ ngữ cảnh trước khi chấm.
    /// </summary>
    public record GetAssignedEvaluationRequestDetailQuery(
        Guid RequestId,
        Guid ExpertId) : IRequest<ResponseDto<ExpertEvaluationRequestDetailDto>>;
}
