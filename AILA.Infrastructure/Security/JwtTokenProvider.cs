using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using Microsoft.Extensions.Options;
using Shared.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Security
{
    public class JwtTokenProvider : ITokenProvider
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenProvider(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateAccessToken(User user)
        {
            var jwtKey = _jwtSettings.Key ?? throw new InvalidOperationException("JWT Key chưa được cấu hình trong appsettings.json.");
            var key = Encoding.UTF8.GetBytes(jwtKey);

            // Tận dụng logic parse an toàn từ code cũ của bạn
            var expireMinutes = int.Parse(_jwtSettings.ExpireMinutes ?? "60");

            // 1. Đóng gói Claims theo chuẩn Domain mới (Dùng Id dạng Guid, Email, và 1 Role duy nhất)
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()) // Hệ thống mới: 1 User chỉ có 1 Role cụ thể
        };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                Issuer = _jwtSettings.Issuer,    // Tận dụng thêm Issuer từ Shared cũ của bạn
                Audience = _jwtSettings.Audience, // Tận dụng thêm Audience từ Shared cũ của bạn
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            // Giữ nguyên logic sinh mã ngẫu nhiên bảo mật cao cực chuẩn của bạn
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
