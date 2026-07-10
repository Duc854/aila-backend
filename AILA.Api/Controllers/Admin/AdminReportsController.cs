using AILA.Api.Extensions;
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

            // Require resolution note for actions that affect content/user
            if ((request.Action == ModerationAction.RemoveContent || request.Action == ModerationAction.SuspendUser || request.Action == ModerationAction.WarnUser)
                && string.IsNullOrWhiteSpace(request.ResolutionNote))
            {
                return BadRequest(ResponseDto<object>.FailResult("MISSING_NOTE", "Ghi chú xử lý là bắt buộc cho hành động này."));
            }

            // Apply action
            switch (request.Action)
            {
                case ModerationAction.DismissReport:
                    // simply mark resolved
                    report.Resolve();
                    // Audit
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
                    // TODO: implement warnings (e.g., create notification). For now just resolve.
                    report.Resolve();
                    _logger.LogInformation("Admin {AdminId} warned user for report {ReportId}. Note: {Note}", identity.UserId, report.Id, request.ResolutionNote);
                    break;
                case ModerationAction.SuspendUser:
                    // Suspend the course owner if possible
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

    public enum ModerationAction
    {
        DismissReport,
        RemoveContent,
        WarnUser,
        SuspendUser
    }

    public record ReviewReportRequest(ModerationAction Action, string? ResolutionNote);
}
