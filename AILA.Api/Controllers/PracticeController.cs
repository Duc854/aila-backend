using AILA.Api.Extensions;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Features.PracticeAttempts.Commands.AbandonAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
using AILA.Application.Features.PracticeAttempts.Queries.GetAttemptDetail;
using AILA.Application.Features.PracticeAttempts.Queries.GetViolations;
using AILA.Application.Features.PracticeMaterials.Queries.GetMaterialDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PracticeController : ControllerBase
{
    private readonly IMediator _mediator;

    public PracticeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Tạo một phiên luyện tập mới (UC-27)
    /// </summary>
    [HttpPost("attempts")]
    [Authorize(Roles = "Learner")]
    public async Task<IActionResult> CreateAttempt([FromBody] CreateAttemptCommand command)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var secureCommand = command with { RequestAccountId = identity.UserId };
        var attemptId = await _mediator.Send(secureCommand);
        return CreatedAtAction(nameof(GetAttemptDetail), new { id = attemptId }, new { Id = attemptId });
    }

    /// <summary>
    /// Xem chi tiết một phiên luyện tập & kết quả đánh giá (UC-28)
    /// </summary>
    [HttpGet("attempts/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PracticeAttemptDto>> GetAttemptDetail(Guid id)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var requestAccountId = (identity.Role == "Admin" || identity.Role == "Expert")
            ? Guid.Empty
            : identity.UserId;

        var result = await _mediator.Send(new GetAttemptDetailQuery(id, requestAccountId));
        return Ok(result);
    }

    /// <summary>
    /// Gửi prompt trong phiên luyện tập (gọi AI)
    /// </summary>
    [HttpPost("attempts/{attemptId:guid}/submit")]
    [Authorize(Roles = "Learner")]
    public async Task<ActionResult<PromptSubmissionDto>> SubmitPrompt(Guid attemptId, [FromBody] SubmitPromptRequest request)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var result = await _mediator.Send(new SubmitPromptCommand(attemptId, request.UserPrompt, identity.UserId));
        return Ok(result);
    }

    /// <summary>
    /// Hoàn thành phiên luyện tập và nhận gợi ý tổng thể từ AI
    /// </summary>
    [HttpPost("attempts/{id:guid}/complete")]
    [Authorize(Roles = "Learner")]
    public async Task<ActionResult<CompleteAttemptResponseDto>> CompleteAttempt(Guid id)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var result = await _mediator.Send(new CompleteAttemptCommand(id, identity.UserId));
        return Ok(result);
    }

    /// <summary>
    /// Bỏ dở phiên luyện tập
    /// </summary>
    [HttpPost("attempts/{id:guid}/abandon")]
    [Authorize(Roles = "Learner")]
    public async Task<IActionResult> AbandonAttempt(Guid id)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        await _mediator.Send(new AbandonAttemptCommand(id, identity.UserId));
        return NoContent();
    }

    // ==================== ENDPOINT PHỤ TRỢ ====================

    /// <summary>
    /// Lấy thông tin chi tiết Material (kịch bản thực hành)
    /// </summary>
    [HttpGet("materials/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<AIPracticeMaterialDetailDto>> GetMaterialDetail(Guid id)
    {
        var result = await _mediator.Send(new GetMaterialDetailQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Xem nhật ký vi phạm của một attempt (cho Admin)
    /// </summary>
    [HttpGet("violations/by-attempt/{attemptId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PromptViolationLogDto>>> GetViolations(Guid attemptId)
    {
        var result = await _mediator.Send(new GetViolationsQuery(attemptId));
        return Ok(result);
    }
}

public class SubmitPromptRequest
{
    public string UserPrompt { get; set; } = string.Empty;
}
