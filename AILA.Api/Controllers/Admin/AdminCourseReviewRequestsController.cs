using AILA.Application.Features.CourseReviewRequests.Commands.ApproveCourseReReview;
using AILA.Application.Features.CourseReviewRequests.Commands.RejectCourseReReview;
using AILA.Application.Features.CourseReviewRequests.Queries.GetCourseReReviewRequests;
using AILA.Application.Features.Materials.Queries.GetMaterialDetail;
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
        var command = new ApproveCourseReReviewCommand(requestId, body?.ReviewComment);
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

        var command = new RejectCourseReReviewCommand(requestId, body.ReviewComment!);
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
    /// Admin xem trước chi tiết một học liệu trong bất kỳ khóa học nào.
    /// Dùng cho CoursePreviewModal ở trang admin (reports, review requests).
    /// GET /api/admin/courses/{courseId}/materials/{materialId}/preview
    /// </summary>
    [HttpGet("/api/admin/courses/{courseId:guid}/materials/{materialId:guid}/preview")]
    public async Task<IActionResult> PreviewMaterial(
        Guid courseId,
        Guid materialId,
        CancellationToken ct)
    {
        var query  = new GetMaterialDetailQuery(courseId, materialId);
        var result = await _sender.Send(query, ct);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}

// Request model
public record ReviewCourseRequestBody(string? ReviewComment);
