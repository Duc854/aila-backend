using AILA.Application.Common.Dtos.AI;
using AILA.Application.Features.PracticeAttempts.Commands.AbandonAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
using AILA.Application.Features.PracticeAttempts.Queries.GetAttemptDetail;
using AILA.Application.Features.PracticeAttempts.Queries.GetViolations;
using AILA.Application.Features.PracticeAttempts.Queries.ListAttempts;
using AILA.Application.Features.PracticeMaterials.Queries.GetMaterialDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
    /// Health check
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { Message = "Practice AI Demo API is running!" });
    }

    /// <summary>
    /// Tạo một phiên luyện tập mới
    /// </summary>
    [HttpPost("attempts")]
    public async Task<IActionResult> CreateAttempt([FromBody] CreateAttemptCommand command)
    {
        var attemptId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAttemptDetail), new { id = attemptId }, new { Id = attemptId });
    }

    /// <summary>
    /// Xem chi tiết một phiên luyện tập
    /// </summary>
    [HttpGet("attempts/{id:guid}")]
    public async Task<ActionResult<PracticeAttemptDto>> GetAttemptDetail(Guid id)
    {
        var result = await _mediator.Send(new GetAttemptDetailQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Liệt kê các phiên luyện tập theo Enrollment
    /// </summary>
    [HttpGet("attempts/by-enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<List<PracticeAttemptDto>>> ListAttempts(Guid enrollmentId)
    {
        var result = await _mediator.Send(new ListAttemptsQuery(enrollmentId));
        return Ok(result);
    }

    /// <summary>
    /// Gửi prompt trong phiên luyện tập (gọi AI)
    /// </summary>
    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<ActionResult<PromptSubmissionDto>> SubmitPrompt(Guid attemptId, [FromBody] SubmitPromptRequest request)
    {
        var result = await _mediator.Send(new SubmitPromptCommand(attemptId, request.UserPrompt));
        return Ok(result);
    }

    /// <summary>
    /// Hoàn thành phiên luyện tập và nhận gợi ý tổng thể từ AI
    /// </summary>
    [HttpPost("attempts/{id:guid}/complete")]
    public async Task<ActionResult<CompleteAttemptResponseDto>> CompleteAttempt(Guid id)
    {
        var result = await _mediator.Send(new CompleteAttemptCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Bỏ dở phiên luyện tập
    /// </summary>
    [HttpPost("attempts/{id:guid}/abandon")]
    public async Task<IActionResult> AbandonAttempt(Guid id)
    {
        await _mediator.Send(new AbandonAttemptCommand(id));
        return NoContent();
    }

    // ==================== 3 ENDPOINT MỚI ====================

    /// <summary>
    /// Lấy thông tin chi tiết Material (kịch bản thực hành)
    /// </summary>
    [HttpGet("materials/{id:guid}")]
    public async Task<ActionResult<AIPracticeMaterialDetailDto>> GetMaterialDetail(Guid id)
    {
        var result = await _mediator.Send(new GetMaterialDetailQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Xem nhật ký vi phạm của một attempt (cho Giáo viên/Admin)
    /// </summary>
    [HttpGet("violations/by-attempt/{attemptId:guid}")]
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
