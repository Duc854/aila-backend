using AILA.Api.Extensions;
using AILA.Application.Features.CourseReviewRequests.Commands.ApproveCourseReReview;
using AILA.Application.Features.CourseReviewRequests.Commands.RejectCourseReReview;
using AILA.Application.Features.CourseReviewRequests.Queries.GetCourseReReviewRequests;
using AILA.Application.Features.Materials.Queries.GetMaterialDetail;
using AILA.Application.Features.Reports.Queries.GetReportsByCourse;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin;

/// <summary>
/// Admin xem và xử lý các yêu cầu xem xét lại khóa học bị khoá.
/// </summary>
[ApiController]
[Route("api/admin/course-review-requests")]
[Authorize(Roles = "Admin")]
public class AdminCourseReviewRequestsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminCourseReviewRequestsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lấy danh sách tất cả yêu cầu, có thể filter theo status.
    /// GET /api/admin/course-review-requests?status=Pending
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] CourseReviewRequestStatus? status,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetCourseReReviewRequestsQuery(status), ct);
        return Ok(result);
    }

    /// <summary>
    /// Admin phê duyệt yêu cầu — course được unlock và published lại.
    /// PATCH /api/admin/course-review-requests/{requestId}/approve
    /// </summary>
    [HttpPatch("{requestId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid requestId,
        [FromBody] ReviewCourseRequestBody? body,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

        var command = new ApproveCourseReReviewCommand(requestId, body?.ReviewComment, identity.UserId);
        var result  = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "REQUEST_NOT_FOUND"  => NotFound(result),
                "ALREADY_PROCESSED"  => BadRequest(result),
                "RESTORE_FAILED"     => BadRequest(result),
                _                    => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Admin từ chối yêu cầu — course vẫn bị khoá, kèm lý do.
    /// PATCH /api/admin/course-review-requests/{requestId}/reject
    /// </summary>
    [HttpPatch("{requestId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid requestId,
        [FromBody] ReviewCourseRequestBody body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body?.ReviewComment))
            return BadRequest(ResponseDto<object>.FailResult(
                "COMMENT_REQUIRED", "Lý do từ chối không được để trống."));

        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

        var command = new RejectCourseReReviewCommand(requestId, body.ReviewComment!, identity.UserId);
        var result  = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "REQUEST_NOT_FOUND"  => NotFound(result),
                "ALREADY_PROCESSED"  => BadRequest(result),
                _                    => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Admin xem tất cả báo cáo liên quan đến một khóa học (tra cứu lịch sử vi phạm
    /// khi xét yêu cầu mở lại).
    /// GET /api/admin/courses/{courseId}/reports
    /// </summary>
    [HttpGet("/api/admin/courses/{courseId:guid}/reports")]
    public async Task<IActionResult> GetReportsByCourse(
        Guid courseId,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetReportsByCourseQuery(courseId), ct);
        return Ok(result);
    }
}

// Request model
public record ReviewCourseRequestBody(string? ReviewComment);
