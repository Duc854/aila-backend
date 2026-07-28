using AILA.Api.Extensions;
using AILA.Application.Features.Tags.Commands.CreateSystemTag;
using AILA.Application.Features.Tags.Commands.RemoveSystemTag;
using AILA.Application.Features.Tags.Commands.ReviewTagVerifications;
using AILA.Application.Features.Tags.Commands.UpdateSystemTag;
using AILA.Application.Features.Tags.Queries.GetPendingTagDetail;
using AILA.Application.Features.Tags.Queries.GetPendingTags;
using AILA.Application.Features.Tags.Queries.GetSystemTags;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/tags")]
    [Authorize(Roles = "Admin")]
    public class AdminTagsController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<AdminTagsController> _logger;

        public AdminTagsController(ISender sender, ILogger<AdminTagsController> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        #region UC-85: Review Tag Verification Requests

        /// <summary>
        /// UC-85: Get list of pending tag verification requests
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingVerificationRequests(
        [FromQuery] string? searchKeyword = null,
        CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetPendingTagsQuery(searchKeyword),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// UC-85: Get tag verification request detail
        /// </summary>
        [HttpGet("pending/{tagId:guid}")]
        public async Task<IActionResult> GetPendingVerificationDetail(
            Guid tagId,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetPendingTagDetailQuery(tagId),
                cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "TAG_NOT_FOUND" || result.ErrorCode == "REQUEST_NOT_FOUND")
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        /// <summary>
        /// UC-85: Review tag verification request (Approve/Reject)
        /// </summary>
        [HttpPost("{tagId:guid}/verify")]
        public async Task<IActionResult> ReviewVerificationRequest(
            Guid tagId,
            [FromBody] ReviewTagVerificationCommand command,
            CancellationToken cancellationToken = default)
        {
            // Gán TagId từ route vào command
            var fullCommand = command with { TagId = tagId };

            var result = await _sender.Send(fullCommand, cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "TAG_NOT_FOUND" || result.ErrorCode == "REQUEST_NOT_FOUND")
                    return NotFound(result);

                if (result.ErrorCode == "INVALID_STATUS" || result.ErrorCode == "MISSING_REJECTION_REASON")
                    return BadRequest(result);

                return BadRequest(result);
            }

            // Log audit
            var identity = HttpContext.GetUserIdentity();
            if (identity != null)
            {
                _logger.LogInformation(
                    "Admin {AdminId} {Action} tag verification request for TagId: {TagId}",
                    identity.UserId,
                    command.Status == TagPublishRequestStatus.Approved ? "approved" : "rejected",
                    tagId);
            }

            return Ok(result);
        }

        #endregion

        #region UC-86: Create System Tag

        /// <summary>
        /// UC-86: Create new system tag
        /// </summary>
        [HttpPost("system")]
        public async Task<IActionResult> CreateSystemTag(
            [FromBody] CreateSystemTagCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(command, cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "EMPTY_NAME")
                    return BadRequest(result);

                if (result.ErrorCode == "DUPLICATE_TAG")
                    return Conflict(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region UC-87: Update System Tag

        /// <summary>
        /// UC-87: Update system tag
        /// </summary>
        [HttpPut("system/{tagId:guid}")]
        public async Task<IActionResult> UpdateSystemTag(
            Guid tagId,
            [FromBody] UpdateSystemTagCommand command,
            CancellationToken cancellationToken = default)
        {
            // Gán TagId từ route vào command
            var fullCommand = command with { TagId = tagId };

            var result = await _sender.Send(fullCommand, cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "TAG_NOT_FOUND")
                    return NotFound(result);

                if (result.ErrorCode == "EMPTY_NAME")
                    return BadRequest(result);

                if (result.ErrorCode == "DUPLICATE_TAG")
                    return Conflict(result);

                if (result.ErrorCode == "TAG_IN_USE" || result.ErrorCode == "NOT_SYSTEM_TAG")
                    return BadRequest(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion
        #region UC-88: Remove System Tag

        /// <summary>
        /// UC-88: Remove system tag
        /// </summary>
        [HttpDelete("system/{tagId:guid}")]
        public async Task<IActionResult> RemoveSystemTag(
            Guid tagId,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new RemoveSystemTagCommand(tagId),
                cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "TAG_NOT_FOUND")
                    return NotFound(result);

                if (result.ErrorCode == "TAG_IN_USE" || result.ErrorCode == "NOT_SYSTEM_TAG")
                    return BadRequest(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region UC-86~88: Get System Tags List

        /// <summary>
        /// UC-86~88: Get list of system tags
        /// </summary>
        [HttpGet("system/search")]
        public async Task<IActionResult> GetSystemTags(
            [FromQuery] string? searchKeyword = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetSystemTagsQuery(searchKeyword),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Get all system tags
        /// </summary>
        [HttpGet("system/all")]
        public async Task<IActionResult> GetAllSystemTags(
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetSystemTagsQuery(null),
                cancellationToken);

            return Ok(result);
        }


        #endregion
    }
}