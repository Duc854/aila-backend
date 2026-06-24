using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.Materials.Commands.MarkMaterialAsCompleted;
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

        /// <summary>
        /// API Đánh dấu hoàn thành một học liệu (Video/Document/Quiz) cho Learner
        /// </summary>
        /// <param name="courseId">Id của khóa học lấy từ Route</param>
        /// <param name="materialId">Id của học liệu lấy từ Route</param>
        /// <returns>ResponseDto dạng bool biểu thị trạng thái xử lý</returns>
        [HttpPost("{materialId}/complete")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> MarkAsComplete([FromRoute] Guid courseId, [FromRoute] Guid materialId)
        {
            // Lấy thông tin định danh sạch sẽ từ JWT Token thông qua HttpContextExtension của bạn.
            // Nhờ cấu hình chặn lỗi 401/403 tự động toàn cục trước đó, tại đây 'identity' chắc chắn không bao giờ null.
            var identity = HttpContext.GetUserIdentity()!;

            // Đóng gói dữ liệu vào bản tin Command
            var command = new MarkMaterialAsCompletedCommand(courseId, materialId, identity.UserId);

            // Gửi sang tầng Application xử lý nghiệp vụ, truyền kèm cả CancellationToken hệ thống
            var result = await _mediator.Send(command, HttpContext.RequestAborted);

            // Nếu thất bại về mặt logic nghiệp vụ (Ví dụ: Học viên chưa đăng ký khóa học, hoặc truyền sai ID học liệu)
            if (!result.Success)
            {
                return BadRequest(result);
            }

            // Trả về dữ liệu thành công kèm wrapper chuẩn: { "success": true, "data": true }
            return Ok(result);
        }
    }
}
