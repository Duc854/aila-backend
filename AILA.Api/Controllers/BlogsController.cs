using AILA.Application.Features.Blogs.Queries.GetBlogDetail;
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
        private readonly IMediator _mediator;

        public BlogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// GET /api/blogs/{id} — Authorization header là tùy chọn (Guest không cần token).
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetBlogDetailQuery(id), ct);

            if (result == null)
                return NotFound(ResponseDto<object>.FailResult("BLOG_NOT_FOUND", "Blog not found."));

            return Ok(ResponseDto<object>.SuccessResult(result));
        }
    }
}
