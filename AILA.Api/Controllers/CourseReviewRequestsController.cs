using AILA.Api.Extensions;
using AILA.Application.Features.CourseReviewRequests.Commands.RequestCourseReReview;
using AILA.Application.Features.CourseReviewRequests.Queries.GetMyReReviewRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

/// <summary>
/// Expert gửi và theo dõi yêu cầu xem xét lại khóa học bị khoá.
/// </summary>
[ApiController]
[Route("api/experts/me/course-review-requests")]
[Authorize(Roles = "Expert")]
public class CourseReviewRequestsController : ControllerBase
{
    private readonly ISender _sender;

    public CourseReviewRequestsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lấy danh sách yêu cầu xem xét lại của Expert đang đăng nhập.
    /// GET /api/experts/me/course-review-requests
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var result = await _sender.Send(new GetMyReReviewRequestsQuery(identity.UserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gửi yêu cầu xem xét lại khóa học đang bị khoá.
    /// POST /api/experts/me/course-review-requests
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateCourseReReviewRequest body,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var command = new RequestCourseReReviewCommand(
            body.CourseId,
            identity.UserId,
            body.Reason);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "COURSE_NOT_FOUND"  => NotFound(result),
                "FORBIDDEN"         => StatusCode(StatusCodes.Status403Forbidden, result),
                "COURSE_NOT_LOCKED" => BadRequest(result),
                "PENDING_EXISTS"    => BadRequest(result),
                _                   => BadRequest(result)
            };
        }

        return Ok(result);
    }
}

// Request model
public record CreateCourseReReviewRequest(Guid CourseId, string Reason);
