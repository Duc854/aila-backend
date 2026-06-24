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
        private readonly ISender _sender;

        public TagsController(ISender sender)
        {
            _sender = sender;
        }

        /// Lấy tất cả tag đã được duyệt 
        [HttpGet]
        public async Task<IActionResult> GetPublishedTags()
        {
            var result = await _sender.Send(new GetPublishedTagsQuery());
            return Ok(ResponseDto<object>.SuccessResult(result));
        }
    }
}
