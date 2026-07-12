using AILA.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ISender _sender;

        public CategoriesController(ISender sender)
        {
            _sender = sender;
        }

        /// Lấy tất cả danh mục đang hoạt động — dùng cho màn Home và filter khóa học
        [HttpGet]
        public async Task<IActionResult> GetActiveCategories()
        {
            var result = await _sender.Send(new GetActiveCategoriesQuery());
            return Ok(ResponseDto<object>.SuccessResult(result));
        }
    }
}
