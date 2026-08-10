using AILA.Application.Features.AdminActivityLogs.Queries.ReviewAdminActivityLogs;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/activity-logs")]
    [Authorize(Roles = "Admin")]
    public class AdminActivityLogsController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminActivityLogsController(
            ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivityLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] AdminAction? action,
            CancellationToken cancellationToken)
        {
            var query = new ReviewAdminActivityLogsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                Action = action
            };

            var result = await _sender.Send(
                query,
                cancellationToken);

            return Ok(result);
        }
    }
}
