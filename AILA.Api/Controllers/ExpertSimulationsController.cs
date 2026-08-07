using AILA.Application.Common.Dtos.AI;
using AILA.Application.Features.ExpertSimulations.Commands.StartSimulation;
using AILA.Application.Features.ExpertSimulations.Dtos;
using AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
using AILA.Application.Features.PracticeAttempts.Queries.GetAttemptDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AILA.Api.Controllers;

[ApiController]
[Route("api/expert/simulations")]
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
        var command = new StartSimulationCommand(request.ExpertId, request.MaterialId);
        var sessionId = await _mediator.Send(command);
        return Ok(new { SimulationSessionId = sessionId, Message = "Khởi tạo phiên thử nghiệm AI Simulation thành công." });
    }

    /// <summary>
    /// UC-60 Step 5-9: Expert gửi tin nhắn tương tác thử nghiệm với AI
    /// </summary>
    [HttpPost("{sessionId:guid}/submit")]
    public async Task<ActionResult<PromptSubmissionDto>> SubmitSimulationPrompt(Guid sessionId, [FromBody] SubmitSimulationPromptRequest request)
    {
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
        var result = await _mediator.Send(new GetAttemptDetailQuery(sessionId));
        return Ok(result);
    }
}
