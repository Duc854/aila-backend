using AILA.Application.Features.ExpertEvaluations.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Queries.GetAssignedEvaluationRequests
{
    /// <summary>
    /// UC-63: hàng chờ các yêu cầu được giao cho chuyên gia đang đăng nhập.
    /// <paramref name="PageSize"/> null nghĩa là dùng kích thước trang mặc định trong cấu hình.
    /// </summary>
    public record GetAssignedEvaluationRequestsQuery(
        Guid ExpertId,
        ExpertEvaluationRequestStatus? Status,
        int PageIndex,
        int? PageSize) : IRequest<ResponseDto<PageResult<ExpertEvaluationRequestSummaryDto>>>;
}
