using Shared.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AILA.Api.Extensions
{
    public static class HttpContextExtensions
    {
        public static UserIdentity? GetUserIdentity(this HttpContext httpContext)
        {
            // 1. Kiểm tra xem User đã được định danh qua JWT Middleware chưa
            if (httpContext.User.Identity?.IsAuthenticated != true)
                return null;

            var user = httpContext.User;

            // 2. Trích xuất UserId (Dạng Guid từ Claim Sub)
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return null; // Token lỗi hoặc không có ID hợp lệ

            // 3. Trích xuất Email và Role duy nhất
            var email = user.FindFirst(ClaimTypes.Email)?.Value
                        ?? user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? string.Empty;

            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // 4. Trả về Object sạch sẽ cho Controller hoặc Application Layer sử dụng
            return new UserIdentity
            {
                UserId = userId,
                Email = email,
                Role = role
            };
        }
    }
}
