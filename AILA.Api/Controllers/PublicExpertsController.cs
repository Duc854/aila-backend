using AILA.Application.Common.Dtos;
using AILA.Application.Features.Experts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    /// <summary>
    /// Public, read-only API cho hồ sơ công khai của Expert. Không yêu cầu xác thực.
    /// </summary>
    [ApiController]
    [Route("api/public/experts")]
    [AllowAnonymous]
    public class PublicExpertsController : ControllerBase
    {
        private readonly ISender _sender;

        public PublicExpertsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy hồ sơ công khai của Expert kèm danh sách khóa học đã xuất bản.
        /// expertId không tồn tại hoặc tài khoản bị vô hiệu hóa → 404 (không tiết lộ sự tồn tại).
        /// </summary>
        [HttpGet("{expertId}/profile")]
        public async Task<IActionResult> GetProfile(Guid expertId, CancellationToken ct)
        {
            var result = await _sender.Send(new GetPublicExpertProfileQuery(expertId), ct);

            if (result is null)
                return NotFound(
                    ResponseDto<object>.FailResult(
                        "EXPERT_NOT_FOUND",
                        "Không tìm thấy hồ sơ chuyên gia."));

            Response.Headers.CacheControl = "public, max-age=300";
            return Ok(ResponseDto<PublicExpertProfileDto>.SuccessResult(result));
        }
    }
}
