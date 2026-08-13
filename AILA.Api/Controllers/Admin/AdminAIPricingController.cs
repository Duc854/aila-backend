using AILA.Application.Features.AIPricing.Dtos;
using AILA.Application.Features.AIPricing.Commands.CreateAIPricingConfig;
using AILA.Application.Features.AIPricing.Commands.UpdateAIPricingConfig;
using AILA.Application.Features.AIPricing.Commands.DeleteAIPricingConfig;
using AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AILA.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/ai-pricing")]
public class AdminAIPricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAIPricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// UC-89: Admin xem cấu hình thông tin đơn giá AI Token hiện tại (Có cờ IsConfigured để UI hiển thị thông báo nếu chưa có giá)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AIPricingListResponseDto>> GetPricingConfigs()
    {
        var result = await _mediator.Send(new GetAIPricingConfigsQuery());
        return Ok(result);
    }

    /// <summary>
    /// UC-89: Admin tạo mới đơn giá cho một Model AI
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AIPricingConfigDto>> CreatePricingConfig([FromBody] CreateAIPricingRequest request)
    {
        var command = new CreateAIPricingConfigCommand(
            ModelId: request.ModelId,
            ServiceName: request.ServiceName,
            CostPerInputToken: request.CostPerInputToken,
            CostPerOutputToken: request.CostPerOutputToken,
            Currency: request.Currency,
            IsActive: request.IsActive);

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPricingConfigs), new { id = result.Id }, result);
    }

    /// <summary>
    /// UC-89: Admin cập nhật đơn giá theo Id cụ thể
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AIPricingConfigDto>> UpdatePricingConfigById(Guid id, [FromBody] UpdateAIPricingRequest request)
    {
        var command = new UpdateAIPricingConfigCommand(
            Id: id,
            ModelId: request.ModelId ?? string.Empty,
            ServiceName: request.ServiceName,
            CostPerInputToken: request.CostPerInputToken,
            CostPerOutputToken: request.CostPerOutputToken,
            Currency: request.Currency,
            IsActive: request.IsActive);

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// UC-89: Admin xóa một cấu hình đơn giá Model AI
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<bool>> DeletePricingConfig(Guid id)
    {
        var result = await _mediator.Send(new DeleteAIPricingConfigCommand(id));
        return Ok(result);
    }
}
