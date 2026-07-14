using AILA.Application.Features.Users.Commands.CreateExpertAccount;
using AILA.Application.Features.Users.Commands.UpdateUserStatus;
using AILA.Application.Features.Users.Dtos;
using AILA.Application.Features.Users.Queries.GetRoles;
using AILA.Application.Features.Users.Queries.GetUserById;
using AILA.Application.Features.Users.Queries.GetUsers;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class UsersManagementController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<UsersManagementController> _logger;

        public UsersManagementController(
            ISender sender,
            ILogger<UsersManagementController> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        #region UC-76: Review User Accounts

        /// <summary>
        /// UC-76: Get list of user accounts with search and filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? searchKeyword = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetUsersQuery(searchKeyword, role, isActive),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// UC-76: Get user account detail
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetUserByIdQuery(id),
                cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "USER_NOT_FOUND")
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region UC-77: Update User Status

        /// <summary>
        /// UC-77: Update user account status (Active/Inactive)
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateUserStatus(
            Guid id,
            [FromBody] UpdateUserStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            var fullCommand = command with { UserId = id };

            var result = await _sender.Send(fullCommand, cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "USER_NOT_FOUND")
                    return NotFound(result);

                return BadRequest(result);
            }

            _logger.LogInformation(
                "Admin updated user status. UserId: {UserId}, IsActive: {IsActive}",
                id,
                command.IsActive);

            return Ok(result);
        }

        #endregion

        #region UC-78: Create Expert Account

        /// <summary>
        /// UC-78: Create new expert account
        /// </summary>
        [HttpPost("experts")]
        public async Task<IActionResult> CreateExpertAccount(
            [FromBody] CreateExpertAccountCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(command, cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorCode == "DUPLICATE_EMAIL")
                    return Conflict(result);

                if (result.ErrorCode == "INVALID_EMAIL" ||
                    result.ErrorCode == "INVALID_PASSWORD" ||
                    result.ErrorCode == "INVALID_FULL_NAME")
                    return BadRequest(result);

                return BadRequest(result);
            }

            _logger.LogInformation(
                "Admin created expert account. UserId: {UserId}, Email: {Email}",
                result.Data?.Id,
                command.Email);

            return Ok(result);
        }

        #endregion

        #region Helper APIs

        /// <summary>
        /// Get list of available roles for filter
        /// </summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetRolesQuery(), cancellationToken);
            return Ok(result);
        }

        #endregion
    }
}