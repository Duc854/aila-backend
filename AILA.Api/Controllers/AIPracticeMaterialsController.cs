using AILA.Api.Extensions;
using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIPracticeMaterialsController : ControllerBase
    {
        private readonly ISender _sender;

        public AIPracticeMaterialsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Tạo AI Practice Scenario mới.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> Create(
            [FromBody] CreateAIPracticeMaterialRequestDto request)
        {
            // Lấy thông tin Expert từ JWT
            var identity = HttpContext.GetUserIdentity();

            if (identity == null)
            {
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "UNAUTHORIZED",
                        "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));
            }

            // Gửi Command
            var command = new CreateAIPracticeMaterialCommand(
                identity.UserId,
                request);

            var result = await _sender.Send(
                command,
                HttpContext.RequestAborted);

            // Xử lý kết quả
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }


        /// <summary>
        /// Lấy thông tin chi tiết AI Practice Scenario để chỉnh sửa.
        /// </summary>
        /// <param name="materialId">Mã Material của AI Practice Scenario.</param>
        [HttpGet("{materialId}/edit")]
        public async Task<IActionResult> GetDetailForEdit(
            [FromRoute] Guid materialId)
        {
            var identity = HttpContext.GetUserIdentity();

            if (identity == null)
            {
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "UNAUTHORIZED",
                        "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));
            }

            var query = new GetAIPracticeMaterialDetailQuery(
                identity.UserId,
                materialId);

            var result = await _sender.Send(
                query,
                HttpContext.RequestAborted);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
