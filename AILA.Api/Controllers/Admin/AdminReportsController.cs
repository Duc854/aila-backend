using AILA.Api.Extensions;
using AILA.Application.Features.Reports.Dtos;
using AILA.Infrastructure.Persistence;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminReportsController> _logger;

        public AdminReportsController(ApplicationDbContext db, ILogger<AdminReportsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetReports([FromQuery] ReportStatus? status)
        {
            var query = _db.Set<ContentReport>().AsQueryable();
            if (status != null)
                query = query.Where(r => r.Status == status.Value);

            var list = query.Select(r => new
            {
                r.Id,
                r.LearnerId,
                r.CourseId,
                r.MaterialId,
                r.ReportType,
                r.Description,
                r.Status,
                r.CreatedAt
            }).ToList();

            return Ok(ResponseDto<object>.SuccessResult(list));
        }

        [HttpGet("{reportId:guid}")]
        public IActionResult GetReportById([FromRoute] Guid reportId)
        {
            var report = _db.Set<ContentReport>().FirstOrDefault(r => r.Id == reportId);
            if (report == null)
                return NotFound(ResponseDto<object>.FailResult("REPORT_NOT_FOUND", "Không tìm thấy báo cáo."));

            var response = new
            {
                report.Id,
                report.LearnerId,
                report.CourseId,
                report.MaterialId,
                report.ReportType,
                report.Description,
                report.Status,
                report.CreatedAt,
                report.ResolvedAt
            };

            return Ok(ResponseDto<object>.SuccessResult(response));
        }

        [HttpPost("{reportId:guid}/action")]
        public IActionResult ReviewReport([FromRoute] Guid reportId, [FromBody] ReviewReportRequest request)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var report = _db.Set<ContentReport>().FirstOrDefault(r => r.Id == reportId);
            if (report == null)
                return NotFound(ResponseDto<object>.FailResult("REPORT_NOT_FOUND", "Không tìm thấy báo cáo."));

            if (report.Status != ReportStatus.Pending)
                return BadRequest(ResponseDto<object>.FailResult("ALREADY_RESOLVED", "Báo cáo đã được xử lý hoặc không thể thực hiện hành động này."));

            if ((request.Action == ModerationAction.RemoveContent || request.Action == ModerationAction.SuspendUser || request.Action == ModerationAction.WarnUser)
                && string.IsNullOrWhiteSpace(request.ResolutionNote))
            {
                return BadRequest(ResponseDto<object>.FailResult("MISSING_NOTE", "Ghi chú xử lý là bắt buộc cho hành động này."));
            }

            switch (request.Action)
            {
                case ModerationAction.DismissReport:
                    report.Resolve();
                    _logger.LogInformation("Admin {AdminId} dismissed report {ReportId}. Note: {Note}", identity.UserId, report.Id, request.ResolutionNote);
                    break;
                case ModerationAction.RemoveContent:
                    if (report.CourseId != null)
                    {
                        var course = _db.Set<Course>().FirstOrDefault(c => c.Id == report.CourseId);
                        if (course != null)
                        {
                            course.Unpublish();
                        }
                    }
                    report.Resolve();
                    _logger.LogInformation("Admin {AdminId} removed content for report {ReportId}. Note: {Note}", identity.UserId, report.Id, request.ResolutionNote);
                    break;
                case ModerationAction.WarnUser:
                    report.Resolve();
                    _logger.LogInformation("Admin {AdminId} warned user for report {ReportId}. Note: {Note}", identity.UserId, report.Id, request.ResolutionNote);
                    break;
                case ModerationAction.SuspendUser:
                    if (report.CourseId != null)
                    {
                        var course = _db.Set<Course>().FirstOrDefault(c => c.Id == report.CourseId);
                        if (course != null)
                        {
                            var owner = _db.Users.FirstOrDefault(u => u.Id == course.ExpertId);
                            if (owner != null) owner.Deactivate();
                        }
                    }
                    report.Resolve();
                    _logger.LogInformation("Admin {AdminId} suspended user for report {ReportId}. Note: {Note}", identity.UserId, report.Id, request.ResolutionNote);
                    break;
                default:
                    return BadRequest(ResponseDto<object>.FailResult("INVALID_ACTION", "Hành động không hợp lệ."));
            }

            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Report processed" }));
        }
    }
}
