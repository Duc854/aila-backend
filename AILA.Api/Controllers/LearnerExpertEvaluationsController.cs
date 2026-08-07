using AILA.Api.Extensions;
using AILA.Application.Features.ExpertEvaluations;
using AILA.Application.Features.ExpertEvaluations.Commands.RequestExpertEvaluation;
using AILA.Application.Features.ExpertEvaluations.Queries.GetLearnerExpertEvaluation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

/// <summary>
/// Học viên nhờ chuyên gia đánh giá bài thực hành và xem lại kết quả (UC-29, UC-30).
/// </summary>
[ApiController]
[Route("api/learner")]
[Authorize(Roles = "Learner")]
public class LearnerExpertEvaluationsController : ControllerBase
{
    private readonly ISender _sender;

    public LearnerExpertEvaluationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// UC-29: gửi yêu cầu nhờ chuyên gia đánh giá một lượt thực hành đã có kết quả AI.
    /// POST /api/learner/practice-attempts/{attemptId}/expert-evaluation-request
    /// </summary>
    [HttpPost("practice-attempts/{attemptId:guid}/expert-evaluation-request")]
    public async Task<IActionResult> RequestEvaluation(Guid attemptId, CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var result = await _sender.Send(
            new RequestExpertEvaluationCommand(attemptId, identity.UserId), ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                ExpertEvaluationErrors.PracticeAttemptNotFound => NotFound(result),
                ExpertEvaluationErrors.QuotaExhausted
                    => StatusCode(StatusCodes.Status403Forbidden, result),
                ExpertEvaluationErrors.AiEvaluationUnavailable
                    or ExpertEvaluationErrors.EvaluationAlreadyRequested
                    or ExpertEvaluationErrors.ExpertUnavailable
                    => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return CreatedAtAction(
            nameof(GetEvaluation),
            new { requestId = result.Data!.RequestId },
            result);
    }

    /// <summary>
    /// UC-30: xem kết quả AI và kết quả chuyên gia của một yêu cầu.
    /// GET /api/learner/expert-evaluation-requests/{requestId}
    /// </summary>
    [HttpGet("expert-evaluation-requests/{requestId:guid}")]
    public async Task<IActionResult> GetEvaluation(Guid requestId, CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var result = await _sender.Send(
            new GetLearnerExpertEvaluationQuery(requestId, identity.UserId), ct);

        // Yêu cầu chưa xong hoặc đã hủy vẫn là 200, chỉ khác ở trường status (AC-30.2/.3).
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
