using AILA.Application.Common.Dtos;
using AILA.Application.Features.Blogs.Queries.GetBlogDetail;
using AILA.Application.Features.Blogs.Queries.GetBlogsWithFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogsController : ControllerBase
    {
        private readonly ISender _sender;

        public BlogsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// GET /api/blogs/{id} — Authorization header là tùy chọn (Guest không cần token).
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new GetBlogDetailQuery(id), ct);

            if (result == null)
                return NotFound(ResponseDto<object>.FailResult("BLOG_NOT_FOUND", "Blog not found."));

            return Ok(ResponseDto<object>.SuccessResult(result));
        }

        /// <summary>
        /// API Lấy danh sách bài viết Blog kèm công cụ Filter, tìm kiếm và phân trang công khai
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogs([FromQuery] GetBlogsWithFilterQuery query)
        {
            var result = await _sender.Send(query, HttpContext.RequestAborted);

            return Ok(result);
        }
    }
}
