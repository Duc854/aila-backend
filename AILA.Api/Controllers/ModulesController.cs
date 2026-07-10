using AILA.Api.Extensions;
using AILA.Application.Features.Modules.Commands;
using AILA.Application.Features.Modules.Dtos;
using AILA.Application.Features.Modules.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [Authorize(Roles = "Expert")]
    [ApiController]
    [Route("api/courses/{courseId:guid}/modules")]
    public class ModulesController : ControllerBase
    {
        private readonly ISender _sender;

        public ModulesController(ISender sender) => _sender = sender;

        // ── GET: Lấy danh sách Module của một Course ───────────────────────

        /// <summary>
        /// Lấy toàn bộ danh sách Chương học của một Khóa học.
        /// Chỉ Expert sở hữu khóa học mới được gọi.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetModules(
            Guid courseId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var query  = new GetModulesByCourseQuery(courseId, identity.UserId);
            var result = await _sender.Send(query, ct);

            if (!result.Success) return Forbid();

            return Ok(result);
        }

        // ── POST: Tạo Module mới ────────────────────────────────────────────

        /// <summary>
        /// Expert tạo một Chương học mới trong Khóa học của mình.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateModule(
            Guid courseId,
            [FromBody] SaveModuleRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var command = new CreateModuleCommand(
                CourseId:    courseId,
                ExpertId:    identity.UserId,
                Title:       request.Title,
                Description: request.Description,
                OrderIndex:  request.OrderIndex);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
                return result.ErrorCode switch
                {
                    "COURSE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };

            return CreatedAtAction(
                nameof(GetModules),
                new { courseId },
                result);
        }

        // ── PUT: Cập nhật thông tin Module ──────────────────────────────────

        /// <summary>
        /// Expert cập nhật tiêu đề và mô tả của một Chương học.
        /// </summary>
        [HttpPut("{moduleId:guid}")]
        public async Task<IActionResult> UpdateModule(
            Guid courseId,
            Guid moduleId,
            [FromBody] SaveModuleRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var command = new UpdateModuleCommand(
                ModuleId:    moduleId,
                ExpertId:    identity.UserId,
                Title:       request.Title,
                Description: request.Description);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };

            return Ok(result);
        }

        // ── DELETE: Xóa Module ──────────────────────────────────────────────

        /// <summary>
        /// Expert xóa một Chương học (kéo theo toàn bộ Material bên trong).
        /// </summary>
        [HttpDelete("{moduleId:guid}")]
        public async Task<IActionResult> DeleteModule(
            Guid courseId,
            Guid moduleId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var command = new DeleteModuleCommand(moduleId, identity.UserId);
            var result  = await _sender.Send(command, ct);

            if (!result.Success)
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };

            return NoContent();
        }

        // ── PATCH: Publish / Unpublish Module ───────────────────────────────

        /// <summary>
        /// Expert công khai Chương học (IsPublished = true).
        /// </summary>
        [HttpPatch("{moduleId:guid}/publish")]
        public async Task<IActionResult> PublishModule(
            Guid courseId,
            Guid moduleId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var command = new SetModulePublishCommand(moduleId, identity.UserId, Publish: true);
            var result  = await _sender.Send(command, ct);

            if (!result.Success)
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };

            return Ok(result);
        }

        /// <summary>
        /// Expert ẩn Chương học (IsPublished = false).
        /// </summary>
        [HttpPatch("{moduleId:guid}/unpublish")]
        public async Task<IActionResult> UnpublishModule(
            Guid courseId,
            Guid moduleId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var command = new SetModulePublishCommand(moduleId, identity.UserId, Publish: false);
            var result  = await _sender.Send(command, ct);

            if (!result.Success)
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };

            return Ok(result);
        }

        // ── PUT: Sắp xếp lại thứ tự Module (Drag & Drop) ───────────────────

        /// <summary>
        /// Expert kéo thả sắp xếp lại thứ tự các Chương trong Khóa học.
        /// Gửi toàn bộ danh sách {ModuleId, NewOrderIndex} sau khi sắp xếp.
        /// </summary>
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderModules(
            Guid courseId,
            [FromBody] ReorderModulesRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

            var command = new ReorderModulesCommand(courseId, identity.UserId, request.Items);
            var result  = await _sender.Send(command, ct);

            if (!result.Success)
                return result.ErrorCode switch
                {
                    "COURSE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };

            return Ok(result);
        }
    }
}
