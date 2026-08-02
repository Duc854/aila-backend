using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.Tags.Commands;
using AILA.Application.Features.Tags.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ISender _sender;

        public TagsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy tất cả tag đã được duyệt — dùng cho màn Home và filter khóa học.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublishedTags()
        {
            var result = await _sender.Send(new GetPublishedTagsQuery());
            return Ok(ResponseDto<object>.SuccessResult(result));
        }

        /// Lấy danh sách tag do Expert đang đăng nhập tạo, kèm trạng thái xét duyệt.
        [HttpGet("me")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetMyTags(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            var result = await _sender.Send(new GetMyTagsQuery(identity.UserId), ct);
            return Ok(ResponseDto<object>.SuccessResult(result));
        }

        /// Expert tạo tag tùy chỉnh. Tag sẽ ở trạng thái chưa duyệt (IsPublished = false).
        [HttpPost("custom")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> CreateCustomTag(
            [FromBody] CreateCustomTagRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            try
            {
                var command = new CreateCustomTagCommand(identity.UserId, request.Name, request.Code);
                var result = await _sender.Send(command, ct);
                return Ok(ResponseDto<ExpertTagDto>.SuccessResult(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("CREATE_TAG_FAILED", ex.Message));
            }
        }

        /// Expert gửi yêu cầu xét duyệt (publish) một tag do mình tạo.
        [HttpPost("{tagId}/request-verification")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> RequestVerification(
            Guid tagId,
            [FromBody] RequestTagVerificationRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            try
            {
                var command = new RequestTagVerificationCommand(tagId, identity.UserId, request.Note);
                var result = await _sender.Send(command, ct);
                return Ok(ResponseDto<ExpertTagDto>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ResponseDto<object>.FailResult("FORBIDDEN", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("REQUEST_FAILED", ex.Message));
            }
        }

        /// Expert hủy yêu cầu xét duyệt đang Pending của một tag do mình tạo.
        [HttpDelete("{tagId}/publish-request")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> DeletePublishRequest(
            Guid tagId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            try
            {
                var command = new DeleteTagPublishRequestCommand(tagId, identity.UserId);
                var result = await _sender.Send(command, ct);
                return Ok(ResponseDto<ExpertTagDto>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ResponseDto<object>.FailResult("FORBIDDEN", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("DELETE_REQUEST_FAILED", ex.Message));
            }
        }

        /// Kiểm tra code tag đã tồn tại trong hệ thống chưa.      
        [HttpGet("check-code")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> CheckCode(
            [FromQuery] string code,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_CODE", "Code tag không được để trống."));

            var exists = await _sender.Send(new CheckTagCodeQuery(code), ct);
            return Ok(ResponseDto<bool>.SuccessResult(exists));
        }

        /// <summary>
        /// Lấy thông tin đầy đủ của tag theo code slug.
        /// Trả về tag nếu tìm thấy (kể cả chưa published), null nếu không tồn tại.
        /// Dùng để khi expert nhập code trùng, frontend có đủ dữ liệu hiển thị confirm "dùng tag này".
        /// </summary>
        [HttpGet("by-code")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetTagByCode(
            [FromQuery] string code,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_CODE", "Code tag không được để trống."));

            var tag = await _sender.Send(new GetTagByCodeQuery(code), ct);
            return Ok(ResponseDto<AILA.Application.Features.Tags.Dtos.TagDto?>.SuccessResult(tag));
        }

        /// <summary>
        /// Expert xóa tag do mình tạo, chưa được publish và không đang được gán vào khóa học nào.
        /// </summary>
        [HttpDelete("{tagId}")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> DeleteCustomTag(
            Guid tagId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            try
            {
                var command = new DeleteCustomTagCommand(tagId, identity.UserId);
                var result = await _sender.Send(command, ct);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ResponseDto<object>.FailResult("FORBIDDEN", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("DELETE_TAG_FAILED", ex.Message));
            }
        }
    }

    // Request models
    public record CreateCustomTagRequest(string Name, string Code);
    public record RequestTagVerificationRequest(string? Note);
}
