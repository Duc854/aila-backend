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
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// Lấy tất cả danh mục đang hoạt động — dùng cho màn Home và filter khóa học
        [HttpGet]
        public async Task<IActionResult> GetActiveCategories()
        {
            var result = await _mediator.Send(new GetActiveCategoriesQuery());
            return Ok(ResponseDto<object>.SuccessResult(result));
        }
    }
}
