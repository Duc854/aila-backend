using AILA.Api.Extensions;
using AILA.Application.Features.ExpertEvaluations;
using AILA.Application.Features.ExpertEvaluations.Commands.ProvideExpertEvaluation;
using AILA.Application.Features.ExpertEvaluations.Queries.GetAssignedEvaluationRequestDetail;
using AILA.Application.Features.ExpertEvaluations.Queries.GetAssignedEvaluationRequests;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

/// <summary>
/// Chuyên gia xem hàng chờ yêu cầu được giao và nộp kết quả đánh giá (UC-63, UC-64).
/// </summary>
[ApiController]
[Route("api/expert/expert-evaluation-requests")]
[Authorize(Roles = "Expert")]
public class ExpertEvaluationRequestsController : ControllerBase
{
    private readonly ISender _sender;

    public ExpertEvaluationRequestsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// UC-63: danh sách yêu cầu được giao cho chuyên gia đang đăng nhập, có phân trang.
    /// GET /api/expert/expert-evaluation-requests
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAssignedRequests(
        [FromQuery] ExpertEvaluationRequestStatus? status,
        [FromQuery] int pageIndex,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var result = await _sender.Send(
            new GetAssignedEvaluationRequestsQuery(identity.UserId, status, pageIndex, pageSize), ct);

        return Ok(result);
    }

    /// <summary>
    /// UC-63: chi tiết một yêu cầu được giao, đủ ngữ cảnh để chấm.
    /// GET /api/expert/expert-evaluation-requests/{requestId}
    /// </summary>
    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> GetRequestDetail(Guid requestId, CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var result = await _sender.Send(
            new GetAssignedEvaluationRequestDetailQuery(requestId, identity.UserId), ct);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// UC-64: nộp điểm và phản hồi, chốt yêu cầu sang trạng thái hoàn tất.
    /// POST /api/expert/expert-evaluation-requests/{requestId}/evaluation
    /// </summary>
    [HttpPost("{requestId:guid}/evaluation")]
    public async Task<IActionResult> ProvideEvaluation(
        Guid requestId,
        [FromBody] ProvideEvaluationRequest body,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var command = new ProvideExpertEvaluationCommand(
            requestId,
            identity.UserId,
            body.OverallScore,
            body.Feedback,
            body.Recommendation);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                ExpertEvaluationErrors.RequestNotFound => NotFound(result),
                ExpertEvaluationErrors.AlreadyEvaluated
                    or ExpertEvaluationErrors.InvalidState
                    => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }
}

/// <summary>
/// Payload UC-64. Recommendation là tuỳ chọn, bỏ trống hoặc không gửi đều hợp lệ.
/// </summary>
public record ProvideEvaluationRequest(
    decimal OverallScore,
    string Feedback,
    string? Recommendation);
