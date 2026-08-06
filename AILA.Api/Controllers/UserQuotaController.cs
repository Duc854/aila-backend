using AILA.Application.Common.Dtos.AI;
using AILA.Application.Features.Quota.Queries.GetAdminUserTokenLogs;
using AILA.Application.Features.Quota.Queries.GetMyQuota;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AILA.Api.Controllers;

[ApiController]
[Route("api/user/quota")]
public class UserQuotaController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserQuotaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Học viên xem hạn mức và số token đã sử dụng hôm nay của mình
    /// </summary>
    [HttpGet("my-status")]
    public async Task<ActionResult<UserQuotaStatusDto>> GetMyQuotaStatus([FromQuery] Guid? accountId)
    {
        // Sử dụng accountId mặc định demo nếu không truyền header/claim
        var targetAccountId = accountId ?? Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var result = await _mediator.Send(new GetMyQuotaQuery(targetAccountId));
        return Ok(result);
    }

    /// <summary>
    /// Học viên xem nhật ký sử dụng Token AI của cá nhân
    /// </summary>
    [HttpGet("my-logs")]
    public async Task<ActionResult<List<AITokenLogDto>>> GetMyTokenLogs([FromQuery] Guid? accountId)
    {
        var targetAccountId = accountId ?? Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var result = await _mediator.Send(new GetAdminUserTokenLogsQuery(targetAccountId));
        return Ok(result);
    }
}
