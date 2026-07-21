using AILA.Application.Features.AdminBlog.Commands.CreateBlog;
using AILA.Application.Features.AdminBlog.Commands.DeleteBlog;
using AILA.Application.Features.AdminBlog.Commands.PublishBlog;
using AILA.Application.Features.AdminBlog.Commands.UnpublishBlog;
using AILA.Application.Features.AdminBlog.Commands.UpdateBlog;
using AILA.Application.Features.AdminBlog.Queries.GetAdminBlogDetail;
using AILA.Application.Features.AdminBlog.Queries.GetAdminBlogList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/admin/blogs")]
    [Authorize(Roles = "Admin")]
    public class AdminBlogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminBlogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách Blog dành cho Admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBlogs(
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(
                new GetAdminBlogListQuery(
                    search,
                    pageNumber,
                    pageSize));

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết Blog
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBlog(Guid id)
        {
            var result = await _mediator.Send(
                new GetAdminBlogDetailQuery(id));

            return Ok(result);
        }

        /// <summary>
        /// Tạo Blog
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBlogCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật Blog
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateBlogCommand command)
        {
            if (id != command.BlogId)
            {
                return BadRequest("Route id does not match BlogId.");
            }

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Xóa Blog
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(
                new DeleteBlogCommand(id));

            return Ok(result);
        }

        /// <summary>
        /// Publish Blog
        /// </summary>
        [HttpPut("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            var result = await _mediator.Send(
                new PublishBlogCommand(id));

            return Ok(result);
        }

        /// <summary>
        /// Unpublish Blog
        /// </summary>
        [HttpPut("{id:guid}/unpublish")]
        public async Task<IActionResult> Unpublish(Guid id)
        {
            var result = await _mediator.Send(
                new UnpublishBlogCommand(id));

            return Ok(result);
        }
    }
}