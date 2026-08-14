using AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;
using AILA.Application.Features.AIReports.Queries.GetAIConsumptionTrend;
using AILA.Application.Features.AIReports.Queries.GetAIServiceBreakdown;
using AILA.Application.Features.AIReports.Queries.GetAITopConsumers;
using AILA.Application.Features.AIReports.Queries.GetAIPolicyViolations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/ai-reports")]
[Authorize(Roles = "Admin")]
public class AdminAIReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAIReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// UC-87: Báo cáo Tổng quan Tiêu thụ Tài nguyên AI & Chi phí ước tính (USD & VND)
    /// </summary>
    [HttpGet("resource-consumption")]
    public async Task<IActionResult> GetResourceConsumptionReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAIResourceConsumptionReportQuery(startDate.ToUtc(), endDate.ToUtcEndOfDay()), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Dashboard Analytics: Dữ liệu xu hướng tiêu thụ Token & Chi phí theo thời gian
    /// </summary>
    [HttpGet("consumption-trend")]
    public async Task<IActionResult> GetConsumptionTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string interval = "day",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAIConsumptionTrendQuery(startDate.ToUtc(), endDate.ToUtcEndOfDay(), interval), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Dashboard Analytics: Cơ cấu tỷ trọng chi phí và Token theo từng dịch vụ
    /// </summary>
    [HttpGet("breakdown-by-service")]
    public async Task<IActionResult> GetServiceBreakdown(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAIServiceBreakdownQuery(startDate.ToUtc(), endDate.ToUtcEndOfDay()), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Dashboard Analytics: Top người dùng tiêu tốn nhiều Token / Chi phí AI nhất
    /// </summary>
    [HttpGet("top-consumers")]
    public async Task<IActionResult> GetTopConsumers(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 5,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAITopConsumersQuery(startDate.ToUtc(), endDate.ToUtcEndOfDay(), top), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// UC-88: Giám sát vi phạm chính sách & an toàn nội dung AI
    /// </summary>
    [HttpGet("policy-violations")]
    public async Task<IActionResult> GetPolicyViolations(
        [FromQuery] string? violationType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAIPolicyViolationsQuery(violationType, pageNumber, pageSize), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

/// <summary>
/// Extension helpers để normalize DateTime từ query string (Kind=Unspecified) → UTC
/// trước khi truyền vào handlers / EF Core / Npgsql.
/// </summary>
internal static class DateTimeExtensions
{
    /// <summary>Start of day in UTC (00:00:00).</summary>
    internal static DateTime? ToUtc(this DateTime? dt)
        => dt is null ? null : DateTime.SpecifyKind(dt.Value.Date, DateTimeKind.Utc);

    /// <summary>End of day in UTC (23:59:59.999).</summary>
    internal static DateTime? ToUtcEndOfDay(this DateTime? dt)
        => dt is null ? null : DateTime.SpecifyKind(dt.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
}
