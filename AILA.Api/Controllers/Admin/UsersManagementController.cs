using AILA.Api.Extensions;
using AILA.Infrastructure.Persistence;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
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
        private readonly ApplicationDbContext _db;

        private readonly ILogger<UsersManagementController> _logger;

        public UsersManagementController(ApplicationDbContext db, ILogger<UsersManagementController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var q = _db.Users.AsQueryable();
            var query = Request.Query["q"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var ql = query.Trim().ToLower();
                q = q.Where(u => u.Email.ToLower().Contains(ql) || u.FullName.ToLower().Contains(ql));
            }

            var users = q.Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                Role = u.Role,
                IsActive = u.IsActive
            }).ToList();

            return Ok(ResponseDto<object>.SuccessResult(users));
        }

        [HttpPut("{id:guid}")]
        public IActionResult ManageUser([FromRoute] Guid id, [FromBody] ManageUserRequest request)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound(ResponseDto<object>.FailResult("USER_NOT_FOUND", "Không tìm thấy tài khoản người dùng."));

            // Validate role
            if (!Enum.IsDefined(typeof(UserRole), request.UserRole))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_ROLE", "Vai trò không hợp lệ."));

            // Prevent admin from removing their own last admin privilege
            if (identity.UserId == id && request.UserRole != UserRole.Admin)
            {
                var otherAdmins = _db.Users.Count(u => u.Role == UserRole.Admin && u.Id != id && u.IsActive);
                if (otherAdmins == 0)
                    return BadRequest(ResponseDto<object>.FailResult("CANNOT_REMOVE_SELF_ADMIN", "Không thể gỡ quyền Admin của chính bạn vì hệ thống sẽ không còn Admin nào."));
            }

            // Apply account status
            switch (request.AccountStatus)
            {
                case AccountStatus.Active:
                    user.Activate();
                    break;
                case AccountStatus.Inactive:
                case AccountStatus.Suspended:
                    user.Deactivate();
                    break;
                default:
                    return BadRequest(ResponseDto<object>.FailResult("INVALID_STATUS", "Trạng thái tài khoản không hợp lệ."));
            }

            // Apply role
            var entry = _db.Entry(user);
            entry.Property("Role").CurrentValue = request.UserRole;
            user.UpdateTimestamp();

            // Audit role/status change
            var identity2 = HttpContext.GetUserIdentity();
            if (identity2 != null)
            {
                _logger.LogInformation("Admin {AdminId} managed user {UserId}. Role={Role}, Status={Status}", identity2.UserId, user.Id, request.UserRole, request.AccountStatus);
            }

            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "User updated" }));
        }
    }

    public enum AccountStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public record ManageUserRequest(AccountStatus AccountStatus, UserRole UserRole);
}
