using AILA.Api.Extensions;
using AILA.Application.Features.VideoMaterials.Queries.GetVideoDetail;
using AILA.Application.Features.VideoMaterials.Commands.UpdateVideoDetail;
using AILA.Application.Features.VideoMaterials.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

[Authorize(Roles = "Expert")]
[ApiController]
[Route("api/video-materials")]
public class VideoMaterialsController : ControllerBase
{
    private readonly ISender _sender;

    public VideoMaterialsController(
        ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lấy thông tin chi tiết Video Material.
    /// </summary>
    [HttpGet("{materialId:guid}")]
    public async Task<IActionResult> GetVideoDetail(
        Guid materialId,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();

        if (identity == null)
        {
            return Unauthorized(
                ResponseDto<object>.FailResult(
                    "AUTH_FAILED",
                    "Xác thực thất bại."));
        }

        var query = new GetVideoDetailQuery(
            materialId,
            identity.UserId);

        var result = await _sender.Send(query, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "VIDEO_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết Video Material.
    /// </summary>
    [HttpPut("{materialId:guid}")]
    public async Task<IActionResult> UpdateVideoDetail(
        Guid materialId,
        [FromBody] UpdateVideoDetailRequest request,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();

        if (identity == null)
        {
            return Unauthorized(
                ResponseDto<object>.FailResult(
                    "AUTH_FAILED",
                    "Xác thực thất bại."));
        }

        var command = new UpdateVideoDetailCommand(
            materialId,
            identity.UserId,
            request.VideoUrl,
            request.DurationSeconds,
            request.Content);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "VIDEO_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}