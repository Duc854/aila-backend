using AILA.Application.Features.AIPricing.Dtos;
using AILA.Application.Features.AIPricing.Commands.UpdateAIPricingConfig;
using AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    /// UC-89 Step 1-2: Admin xem cấu hình thông tin đơn giá AI Token hiện tại
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AIPricingConfigDto>>> GetPricingConfigs()
    {
        var result = await _mediator.Send(new GetAIPricingConfigsQuery());
        return Ok(result);
    }

    /// <summary>
    /// UC-89 Step 3-6: Admin cập nhật thông tin đơn giá AI Token
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<AIPricingConfigDto>> UpdatePricingConfig([FromBody] UpdateAIPricingRequest request)
    {
        var command = new UpdateAIPricingConfigCommand(
            Id: null,
            ModelId: request.ModelId,
            ServiceName: request.ServiceName,
            CostPerInputToken: request.CostPerInputToken,
            CostPerOutputToken: request.CostPerOutputToken,
            Currency: request.Currency,
            IsActive: request.IsActive);

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// UC-89 Step 3-6: Admin cập nhật đơn giá theo Id cụ thể
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AIPricingConfigDto>> UpdatePricingConfigById(Guid id, [FromBody] UpdateAIPricingRequest request)
    {
        var command = new UpdateAIPricingConfigCommand(
            Id: id,
            ModelId: request.ModelId,
            ServiceName: request.ServiceName,
            CostPerInputToken: request.CostPerInputToken,
            CostPerOutputToken: request.CostPerOutputToken,
            Currency: request.Currency,
            IsActive: request.IsActive);

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
