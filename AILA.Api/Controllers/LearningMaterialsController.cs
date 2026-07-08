using AILA.Api.Extensions;
using AILA.Application.Features.LearningMaterials.Commands.CreateLearningMaterial;
using AILA.Application.Features.LearningMaterials.Commands.DeleteLearningMaterial;
using AILA.Application.Features.LearningMaterials.Commands.ReorderLearningMaterials;
using AILA.Application.Features.LearningMaterials.Commands.UpdateLearningMaterial;
using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Application.Features.LearningMaterials.Queries.GetLearningMaterialsByModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
namespace AILA.Api.Controllers

{
    [Authorize(Roles = "Expert")]
    [ApiController]
    [Route("api/modules/{moduleId:guid}/learning-materials")]
    public class LearningMaterialsController : ControllerBase
    {
        private readonly ISender _sender;

        public LearningMaterialsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy danh sách học liệu của một chương.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLearningMaterials(
            Guid moduleId,
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

            var query = new GetLearningMaterialsByModuleQuery(
                moduleId,
                identity.UserId);

            var result = await _sender.Send(query, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
        /// <summary>
        /// Thêm học liệu mới vào chương.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateLearningMaterial(
            Guid moduleId,
            [FromBody] SaveLearningMaterialRequest request,
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

            var command = new CreateLearningMaterialCommand(
                moduleId,
                identity.UserId,
                request.Title,
                request.MaterialType);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return CreatedAtAction(
                nameof(GetLearningMaterials),
                new { moduleId },
                result);
        }

        /// <summary>
        /// Expert cập nhật tiêu đề học liệu.
        /// </summary>
        [HttpPut("{materialId:guid}")]
        public async Task<IActionResult> UpdateLearningMaterial(
            Guid moduleId,
            Guid materialId,
            [FromBody] UpdateLearningMaterialRequest request,
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

            var command = new UpdateLearningMaterialCommand(
                MaterialId: materialId,
                ExpertId: identity.UserId,
                Title: request.Title);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "MATERIAL_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Expert xóa học liệu.
        /// </summary>
        [HttpDelete("{materialId:guid}")]
        public async Task<IActionResult> DeleteLearningMaterial(
            Guid moduleId,
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

            var command = new DeleteLearningMaterialCommand(
                materialId,
                identity.UserId);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "MATERIAL_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return NoContent();
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderLearningMaterials(
    Guid moduleId,
    [FromBody] ReorderLearningMaterialsRequest request,
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

            var command = new ReorderLearningMaterialsCommand(
                moduleId,
                identity.UserId,
                request.Items);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "MODULE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
    

}
