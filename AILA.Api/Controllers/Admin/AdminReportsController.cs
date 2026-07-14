using AILA.Application.Features.Reports.Commands.ResolveReport;
using AILA.Application.Features.Reports.Queries.GetReportById;
using AILA.Application.Features.Reports.Queries.GetReports;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminReportsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] ReportStatus? status,
            [FromQuery] bool? isCourseReport)
        {
            var result = await _sender.Send(
                new GetReportsQuery(status, isCourseReport));

            return Ok(result);
        }

        [HttpGet("{reportId:guid}")]
        public async Task<IActionResult> GetReportById(Guid reportId)
        {
            var result = await _sender.Send(
                new GetReportByIdQuery(reportId));

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpPatch("{reportId:guid}/resolve")]
        public async Task<IActionResult> ResolveReport(Guid reportId)
        {
            var result = await _sender.Send(
                new ResolveReportCommand(reportId));

            if (!result.Success)
            {
                if (result.ErrorCode == "REPORT_NOT_FOUND")
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}