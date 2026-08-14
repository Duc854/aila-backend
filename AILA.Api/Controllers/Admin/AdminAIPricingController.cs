using AILA.Application.Features.AIPricing.Dtos;
using AILA.Application.Features.AIPricing.Commands.CreateAIPricingConfig;
using AILA.Application.Features.AIPricing.Commands.UpdateAIPricingConfig;
using AILA.Application.Features.AIPricing.Commands.DeleteAIPricingConfig;
using AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/ai-pricing")]
[Authorize(Roles = "Admin")]
public class AdminAIPricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAIPricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// UC-89: Admin xem danh sách cấu hình đơn giá AI Token
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPricingConfigs(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAIPricingConfigsQuery(), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// UC-89: Admin tạo mới đơn giá cho một Model AI
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePricingConfig(
        [FromBody] CreateAIPricingRequest request,
        CancellationToken ct)
    {
        var command = new CreateAIPricingConfigCommand(
            ModelId:            request.ModelId,
            ServiceName:        request.ServiceName,
            CostPerInputToken:  request.CostPerInputToken,
            CostPerOutputToken: request.CostPerOutputToken,
            Currency:           request.Currency,
            IsActive:           request.IsActive);

        var result = await _mediator.Send(command, ct);

        if (!result.Success)
            return result.ErrorCode == "DUPLICATE_MODEL" ? Conflict(result) : BadRequest(result);

        return CreatedAtAction(nameof(GetPricingConfigs), result);
    }

    /// <summary>
    /// UC-89: Admin cập nhật đơn giá theo Id
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePricingConfig(
        Guid id,
        [FromBody] UpdateAIPricingRequest request,
        CancellationToken ct)
    {
        var command = new UpdateAIPricingConfigCommand(
            Id:                 id,
            ModelId:            request.ModelId ?? string.Empty,
            ServiceName:        request.ServiceName,
            CostPerInputToken:  request.CostPerInputToken,
            CostPerOutputToken: request.CostPerOutputToken,
            Currency:           request.Currency,
            IsActive:           request.IsActive);

        var result = await _mediator.Send(command, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// UC-89: Admin xóa một cấu hình đơn giá Model AI
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePricingConfig(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAIPricingConfigCommand(id), ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
