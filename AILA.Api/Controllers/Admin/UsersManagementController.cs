using AILA.Api.Extensions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using AILA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System.ComponentModel.DataAnnotations;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class UsersManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsersManagementController> _logger;
        private readonly IPasswordHasher _passwordHasher;

        public UsersManagementController(
            ApplicationDbContext db,
            ILogger<UsersManagementController> logger,
            IPasswordHasher passwordHasher)
        {
            _db = db;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _db.Users
                .OrderBy(u => u.FullName)
                .Select(u => new UserListItemResponse(
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsActive ? AccountStatus.Active : AccountStatus.Inactive))
                .ToList();

            return Ok(ResponseDto<object>.SuccessResult(users));
        }




        [HttpGet("filter")]
        public IActionResult FilterUsers(
       [FromQuery] string? searchKeyword,
       [FromQuery] AccountStatus? accountStatus,
       [FromQuery] UserRole? userRole)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var keyword = searchKeyword.Trim().ToLowerInvariant();

                query = query.Where(u =>
                    u.Email.ToLower().Contains(keyword) ||
                    u.FullName.ToLower().Contains(keyword));
            }

            if (accountStatus.HasValue)
            {
                query = accountStatus.Value switch
                {
                    AccountStatus.Active => query.Where(u => u.IsActive),
                    AccountStatus.Inactive => query.Where(u => !u.IsActive),
                    AccountStatus.Suspended => query.Where(u => !u.IsActive),
                    _ => query
                };
            }

            if (userRole.HasValue)
            {
                query = query.Where(u => u.Role == userRole.Value);
            }

            var users = query
                .OrderBy(u => u.FullName)
                .Select(u => new UserListItemResponse(
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsActive ? AccountStatus.Active : AccountStatus.Inactive))
                .ToList();

            return Ok(ResponseDto<object>.SuccessResult(users));
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetUserById(Guid id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(ResponseDto<object>.FailResult("USER_NOT_FOUND", "Không tìm thấy tài khoản người dùng."));
            }

            var response = new UserManagementResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive ? AccountStatus.Active : AccountStatus.Inactive);

            return Ok(ResponseDto<object>.SuccessResult(response));
        }

        [HttpPut("{id:guid}")]
        public IActionResult UpdateUser([FromRoute] Guid id, [FromBody] ManageUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var targetUserId = request.UserId ?? id;
            if (targetUserId == Guid.Empty)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_USER_ID", "User Id không hợp lệ."));

            if (request.UserId.HasValue && request.UserId.Value != id)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_USER_ID", "User Id không khớp với route."));

            var user = _db.Users.FirstOrDefault(u => u.Id == targetUserId);
            if (user == null)
                return NotFound(ResponseDto<object>.FailResult("USER_NOT_FOUND", "Không tìm thấy tài khoản người dùng."));

            if (!Enum.IsDefined(typeof(UserRole), request.UserRole!.Value))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_ROLE", "Vai trò không hợp lệ."));

            if (identity.UserId == targetUserId && request.UserRole.Value != UserRole.Admin)
            {
                var otherAdmins = _db.Users.Count(u => u.Role == UserRole.Admin && u.Id != targetUserId && u.IsActive);
                if (otherAdmins == 0)
                    return BadRequest(ResponseDto<object>.FailResult("CANNOT_REMOVE_SELF_ADMIN", "Không thể gỡ quyền Admin của chính bạn vì hệ thống sẽ không còn Admin nào."));
            }

            switch (request.AccountStatus!.Value)
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

            var entry = _db.Entry(user);
            entry.Property("Role").CurrentValue = request.UserRole.Value;
            user.UpdateTimestamp();

            _logger.LogInformation(
                "Admin {AdminId} managed user {UserId}. Role={Role}, Status={Status}",
                identity.UserId,
                user.Id,
                request.UserRole.Value,
                request.AccountStatus.Value);

            _db.SaveChanges();

            var updatedUser = new UserManagementResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive ? AccountStatus.Active : AccountStatus.Inactive);

            return Ok(ResponseDto<object>.SuccessResult(new
            {
                Message = "User updated successfully",
                User = updatedUser
            }));
        }

        [HttpPost("experts")]
        public IActionResult CreateExpertAccount([FromBody] CreateExpertAccountRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_FULL_NAME", "Họ tên không được để trống."));

            if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_EMAIL", "Email không hợp lệ."));

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_PASSWORD", "Mật khẩu không được để trống."));

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            if (_db.Users.Any(u => u.Email == normalizedEmail))
                return Conflict(ResponseDto<object>.FailResult("DUPLICATE_EMAIL", "Email đã tồn tại."));

            var passwordHash = _passwordHasher.HashPassword(request.Password);
            var user = new User(normalizedEmail, request.FullName.Trim(), UserRole.Expert, passwordHash);

            _db.Users.Add(user);
            _db.SaveChanges();

            var expertProfile = new Expert(user.Id);
            _db.Experts.Add(expertProfile);
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new
            {
                Message = "Expert account created successfully",
                User = new UserManagementResponse(
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role,
                    user.IsActive ? AccountStatus.Active : AccountStatus.Inactive)
            }));
        }

        [HttpPatch("{id:guid}/status")]
        public IActionResult UpdateUserStatus([FromRoute] Guid id, [FromBody] UpdateUserStatusRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            if (id == Guid.Empty)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_USER_ID", "User Id không hợp lệ."));

            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound(ResponseDto<object>.FailResult("USER_NOT_FOUND", "Không tìm thấy tài khoản người dùng."));

            if (identity.UserId == id && request.AccountStatus == AccountStatus.Inactive)
            {
                var otherAdmins = _db.Users.Count(u => u.Role == UserRole.Admin && u.Id != id && u.IsActive);
                if (otherAdmins == 0)
                    return BadRequest(ResponseDto<object>.FailResult("CANNOT_DEACTIVATE_SELF_ADMIN", "Không thể vô hiệu hóa tài khoản Admin của chính bạn."));
            }

            switch (request.AccountStatus)
            {
                case AccountStatus.Active:
                    user.Activate();
                    break;
                case AccountStatus.Inactive:
                    user.Deactivate();
                    break;
                default:
                    return BadRequest(ResponseDto<object>.FailResult("INVALID_STATUS", "Trạng thái tài khoản không hợp lệ."));
            }

            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new
            {
                Message = "User status updated successfully",
                User = new UserManagementResponse(
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role,
                    user.IsActive ? AccountStatus.Active : AccountStatus.Inactive)
            }));
        }

        [HttpGet("roles")]
        public IActionResult GetRoles()
        {
            var roles = Enum.GetValues<UserRole>()
                .Select(role => new { Value = (int)role, Name = role.ToString() })
                .ToList();

            return Ok(ResponseDto<object>.SuccessResult(roles));
        }
    }
}
