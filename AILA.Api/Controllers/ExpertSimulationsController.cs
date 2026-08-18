using AILA.Api.Extensions;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Features.ExpertSimulations.Commands.StartSimulation;
using AILA.Application.Features.ExpertSimulations.Dtos;
using AILA.Application.Features.ExpertSimulations.Queries.GetSimulationDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System;
using System.Threading.Tasks;

namespace AILA.Api.Controllers;

[ApiController]
[Route("api/expert/simulations")]
[Authorize(Roles = "Expert")]
public class ExpertSimulationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExpertSimulationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// UC-60 Step 1-4: Expert Khởi tạo một phiên thử nghiệm AI Practice Simulation
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartSimulation([FromBody] StartSimulationRequest request)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var command = new StartSimulationCommand(identity.UserId, request.MaterialId);
        var sessionId = await _mediator.Send(command);
        return Ok(new { SimulationSessionId = sessionId, Message = "Khởi tạo phiên thử nghiệm AI Simulation thành công." });
    }

    /// <summary>
    /// UC-60 Step 5-9: Expert gửi tin nhắn tương tác thử nghiệm với AI
    /// </summary>
    [HttpPost("{sessionId:guid}/submit")]
    public async Task<ActionResult<PromptSubmissionDto>> SubmitSimulationPrompt(Guid sessionId, [FromBody] SubmitSimulationPromptRequest request)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var command = new AILA.Application.Features.ExpertSimulations.Commands.SubmitSimulationPrompt.SubmitSimulationPromptCommand(sessionId, request.UserPrompt);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// UC-60 Step 12-14: Expert Kết thúc thử nghiệm & nhận kết quả đánh giá thử nghiệm từ AI
    /// </summary>
    [HttpPost("{sessionId:guid}/finish")]
    public async Task<ActionResult<CompleteAttemptResponseDto>> FinishSimulation(Guid sessionId)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var command = new AILA.Application.Features.ExpertSimulations.Commands.CompleteSimulation.CompleteSimulationCommand(sessionId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// UC-60: Xem lại thông tin chi tiết phiên thử nghiệm Simulation của Expert
    /// </summary>
    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<PracticeAttemptDto>> GetSimulationDetail(Guid sessionId)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity == null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var result = await _mediator.Send(new GetSimulationDetailQuery(sessionId));
        return Ok(result);
    }
}
