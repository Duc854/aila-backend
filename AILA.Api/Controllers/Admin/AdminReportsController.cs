using AILA.Application.Features.Reports.Commands.DismissReport;
using AILA.Application.Features.Reports.Commands.LockCourseFromReport;
using AILA.Application.Features.Reports.Commands.ResolveReport;
using AILA.Application.Features.Reports.Commands.UnlockCourse;
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

        /// <summary>
        /// Admin từ chối (bác bỏ) báo cáo — nội dung không vi phạm.
        /// Status DB vẫn là Resolved vì domain không có Rejected.
        /// PATCH /api/admin/reports/{reportId}/dismiss
        /// </summary>
        [HttpPatch("{reportId:guid}/dismiss")]
        public async Task<IActionResult> DismissReport(
            Guid reportId,
            [FromBody] DismissReportRequest? body)
        {
            var result = await _sender.Send(
                new DismissReportCommand(reportId, body?.Note));

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "REPORT_NOT_FOUND" => NotFound(result),
                    "ALREADY_RESOLVED" => BadRequest(result),
                    _                  => BadRequest(result)
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Admin lock course liên quan đến report và resolve report cùng lúc.
        /// PATCH /api/admin/reports/{reportId}/lock-course
        /// </summary>
        [HttpPatch("{reportId:guid}/lock-course")]
        public async Task<IActionResult> LockCourseFromReport(Guid reportId)
        {
            var result = await _sender.Send(
                new LockCourseFromReportCommand(reportId));

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "REPORT_NOT_FOUND"  => NotFound(result),
                    "NOT_COURSE_REPORT" => BadRequest(result),
                    "ALREADY_RESOLVED"  => BadRequest(result),
                    _                   => BadRequest(result)
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Admin gỡ khoá course để expert có thể publish lại.
        /// PATCH /api/admin/courses/{courseId}/unlock
        /// Route nằm ở đây (AdminReportsController) vì unlock là action moderation.
        /// </summary>
        [HttpPatch("/api/admin/courses/{courseId:guid}/unlock")]
        public async Task<IActionResult> UnlockCourse(Guid courseId)
        {
            var result = await _sender.Send(
                new UnlockCourseCommand(courseId));

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "COURSE_NOT_FOUND" => NotFound(result),
                    "NOT_LOCKED"       => BadRequest(result),
                    _                  => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }

    // Request model
    public record DismissReportRequest(string? Note);
}