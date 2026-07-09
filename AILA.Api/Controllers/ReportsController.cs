using AILA.Api.Extensions;
using AILA.Application.Features.Reports.Commands.ReportCourse;
using AILA.Application.Features.Reports.Dtos;
using AILA.Application.Features.Reports.Queries.GetReportReasons;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId:guid}/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ISender _sender;

        public ReportsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// UC-33, AC-1 — Danh sách lý do báo cáo hợp lệ để dựng form. Reason được nộp dưới dạng số (Value).
        /// </summary>
        [HttpGet("reasons")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetReasons([FromRoute] Guid courseId, CancellationToken ct)
        {
            var result = await _sender.Send(new GetReportReasonsQuery(), ct);
            return Ok(result);
        }

        /// <summary>
        /// UC-33 — Learner (đã enroll) báo cáo khóa học đang xem. Tạo report Pending và đẩy vào
        /// moderation queue cho Admin (AC-3/AC-4). Validate reason bắt buộc ở server (AC-2/BR-01).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> ReportCourse(
            [FromRoute] Guid courseId,
            [FromBody] ReportCourseRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));

            if (request == null)
                return BadRequest(ResponseDto<object>.FailResult(
                    "VALIDATION_ERROR", "Nội dung báo cáo không được để trống."));

            var command = new ReportCourseCommand(
                courseId, identity.UserId, request.Reason, request.Description);

            var result = await _sender.Send(command, ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        private IActionResult MapError<T>(string? errorCode, ResponseDto<T> result) => errorCode switch
        {
            "COURSE_NOT_FOUND" => NotFound(result),
            "NOT_ENROLLED" => StatusCode(StatusCodes.Status403Forbidden, result),
            "ALREADY_REPORTED" => Conflict(result),
            "INVALID_REASON" => BadRequest(result),
            "VALIDATION_ERROR" => BadRequest(result),
            _ => BadRequest(result)
        };
    }
}
