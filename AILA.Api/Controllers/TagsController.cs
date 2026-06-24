using AILA.Application.Features.Tags.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TagsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// Lấy tất cả tag đã được duyệt 
        [HttpGet]
        public async Task<IActionResult> GetPublishedTags()
        {
            var result = await _mediator.Send(new GetPublishedTagsQuery());
            return Ok(ResponseDto<object>.SuccessResult(result));
        }
    }
}
