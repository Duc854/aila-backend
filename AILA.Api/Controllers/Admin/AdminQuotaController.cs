using AILA.Application.Common.Dtos.AI;
using AILA.Application.Features.Quota.Commands.UpdateUserQuotaLimit;
using AILA.Application.Features.Quota.Queries.GetAdminUserQuotas;
using AILA.Application.Features.Quota.Queries.GetAdminUserTokenLogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AILA.Api.Controllers;

[ApiController]
[Route("api/admin/quota")]
public class AdminQuotaController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminQuotaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Admin xem danh sách hạn mức và lượng Token tiêu tốn của tất cả Học viên
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<UserQuotaStatusDto>>> GetAllUserQuotas()
    {
        var result = await _mediator.Send(new GetAdminUserQuotasQuery());
        return Ok(result);
    }

    /// <summary>
    /// Admin xem nhật ký Token AI chi tiết của một Học viên cụ thể
    /// </summary>
    [HttpGet("users/{accountId:guid}/logs")]
    public async Task<ActionResult<List<AITokenLogDto>>> GetUserTokenLogs(Guid accountId)
    {
        var result = await _mediator.Send(new GetAdminUserTokenLogsQuery(accountId));
        return Ok(result);
    }

    /// <summary>
    /// Admin điều chỉnh / nâng hạn mức Token DailyLimit và MonthlyLimit cho 1 Học viên
    /// </summary>
    [HttpPut("users/{accountId:guid}/limit")]
    public async Task<ActionResult<UserQuotaStatusDto>> UpdateUserLimit(Guid accountId, [FromBody] UpdateQuotaLimitRequestDto request)
    {
        var result = await _mediator.Send(new UpdateUserQuotaLimitCommand(accountId, request.DailyLimit, request.MonthlyLimit));
        return Ok(result);
    }
}
