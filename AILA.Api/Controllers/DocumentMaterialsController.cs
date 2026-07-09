using AILA.Api.Extensions;
using AILA.Application.Features.DocumentMaterials.Commands.UpdateDocumentDetail;
using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Application.Features.DocumentMaterials.Queries.GetDocumentDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [Authorize(Roles = "Expert")]
    [ApiController]
    [Route("api/document-materials")]
    public class DocumentMaterialsController : ControllerBase
    {
        private readonly ISender _sender;

        public DocumentMaterialsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy thông tin chi tiết Document Material.
        /// </summary>
        [HttpGet("{materialId:guid}")]
        public async Task<IActionResult> GetDocumentDetail(
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

            var query = new GetDocumentDetailQuery(
                materialId,
                identity.UserId);

            var result = await _sender.Send(query, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "DOCUMENT_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật nội dung Document Material.
        /// </summary>
        [HttpPut("{materialId:guid}")]
        public async Task<IActionResult> UpdateDocumentDetail(
            Guid materialId,
            [FromBody] UpdateDocumentDetailRequest request,
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

            var command = new UpdateDocumentDetailCommand(
                MaterialId: materialId,
                ExpertId: identity.UserId,
                Content: request.Content);

            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "DOCUMENT_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
}