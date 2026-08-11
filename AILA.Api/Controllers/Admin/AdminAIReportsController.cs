using AILA.Application.Features.AIReports.Dtos;
using AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;
using AILA.Application.Features.AIReports.Queries.GetAIConsumptionTrend;
using AILA.Application.Features.AIReports.Queries.GetAIServiceBreakdown;
using AILA.Application.Features.AIReports.Queries.GetAITopConsumers;
using AILA.Application.Features.AIReports.Queries.GetAIPolicyViolations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AILA.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/ai-reports")]
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
    public async Task<ActionResult<AIResourceConsumptionReportDto>> GetResourceConsumptionReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = new GetAIResourceConsumptionReportQuery(startDate, endDate);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Dashboard Analytics: Dữ liệu vẽ biểu đồ xu hướng tiêu thụ Token & Chi phí theo thời gian (Ngày / Tuần / Tháng)
    /// </summary>
    [HttpGet("consumption-trend")]
    public async Task<ActionResult<AIConsumptionTrendResponseDto>> GetConsumptionTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string interval = "day")
    {
        var query = new GetAIConsumptionTrendQuery(startDate, endDate, interval);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Dashboard Analytics: Cơ cấu tỷ trọng chi phí và Token theo từng dịch vụ/tính năng (Thực hành, Mô phỏng, Chấm điểm, RAG)
    /// </summary>
    [HttpGet("breakdown-by-service")]
    public async Task<ActionResult<AIServiceBreakdownResponseDto>> GetServiceBreakdown(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = new GetAIServiceBreakdownQuery(startDate, endDate);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Dashboard Analytics: Top người dùng và Top bài học tiêu tốn nhiều Token / Chi phí AI nhất
    /// </summary>
    [HttpGet("top-consumers")]
    public async Task<ActionResult<AITopConsumersResponseDto>> GetTopConsumers(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 5)
    {
        var query = new GetAITopConsumersQuery(startDate, endDate, top);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// UC-88: Giám sát vi phạm chính sách & an toàn nội dung AI
    /// </summary>
    [HttpGet("policy-violations")]
    public async Task<ActionResult<PaginatedViolationListDto>> GetPolicyViolations(
        [FromQuery] string? violationType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAIPolicyViolationsQuery(violationType, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
