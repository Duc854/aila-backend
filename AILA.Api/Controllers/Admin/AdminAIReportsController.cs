using AILA.Application.Features.AIReports.Dtos;
using AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;
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
    /// UC-87: Review AI Resource Consumption Reports & Estimated Costs
    /// Admin xem báo cáo tiêu thụ tài nguyên AI và chi phí dịch vụ ước tính
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
    /// UC-88: Review AI Policy Violations Monitoring
    /// Admin xem danh sách vết vi phạm chính sách / an toàn nội dung AI
    /// </summary>
    [HttpGet("policy-violations")]
    public async Task<ActionResult<PaginatedViolationListDto>> GetPolicyViolations(
        [FromQuery] string? violationType,
        [FromQuery] string? severity,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAIPolicyViolationsQuery(violationType, severity, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
