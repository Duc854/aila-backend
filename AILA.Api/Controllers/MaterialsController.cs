using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.Materials.Queries.GetMaterialDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MaterialsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// API lấy thông tin chi tiết của một học liệu (Material) thuộc một khóa học (Course) cho Learner học
        /// </summary>
        /// <param name="courseId">Mã định danh của khóa học</param>
        /// <param name="materialId">Mã định danh của học liệu cần lấy chi tiết</param>
        [HttpGet("{materialId}")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetMaterialDetail([FromRoute] Guid courseId, [FromRoute] Guid materialId)
        {
            // 1. Kiểm tra thông tin định danh (Token) của người dùng
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
            {
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED",
                    "Xác thực người dùng thất bại hoặc mã token không hợp lệ."
                ));
            }

            // 2. Gửi Query sang tầng Application xử lý thông qua MediatR
            var query = new GetMaterialDetailQuery(courseId, materialId);
            var result = await _mediator.Send(query);

            // 3. Kiểm tra kết quả nghiệp vụ từ Handler trả về
            if (!result.Success)
            {
                // Nếu không tìm thấy học liệu hoặc học liệu không thuộc khóa học này, trả về NotFound kèm thông báo từ tầng dưới
                return NotFound(result);
            }

            // 4. Trả về thông tin chi tiết học liệu dạng 200 OK bọc trong ResponseDto
            return Ok(result);
        }
    }
}
