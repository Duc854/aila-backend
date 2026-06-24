using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace AILA.Application.Features.Auth.Commands
{
    public class AdminLoginCommandHandler
        : IRequestHandler<AdminLoginCommand, LoginResponseDto?>
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenProvider _tokenProvider;

        public AdminLoginCommandHandler(
            IConfiguration configuration,
            ITokenProvider tokenProvider)
        {
            _configuration = configuration;
            _tokenProvider = tokenProvider;
        }

        public Task<LoginResponseDto?> Handle(
            AdminLoginCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Đọc thông tin Admin từ appsettings (AdminCredentials section)
            var adminUsername = _configuration["AdminCredentials:Username"];
            var adminPassword = _configuration["AdminCredentials:Password"];

            // 2. So sánh trực tiếp (plain-text vì đây là config tĩnh, không hash)
            if (request.Username != adminUsername || request.Password != adminPassword)
                return Task.FromResult<LoginResponseDto?>(null);

            // 3. Tạo một User object "ảo" đại diện cho Admin để GenerateAccessToken
            //    (không lưu vào DB — Admin không phải entity trong hệ thống)
            var adminVirtualUser = new User(
                email:        "admin@aila.internal",
                fullName:     "Administrator",
                role:         UserRole.Admin,
                passwordHash: null);

            // 4. Phát hành token
            var accessToken  = _tokenProvider.GenerateAccessToken(adminVirtualUser);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            var response = new LoginResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                Role         = UserRole.Admin.ToString(),
                UserId       = adminVirtualUser.Id,
                FullName     = "Administrator",
                Email        = "admin@aila.internal"
            };

            return Task.FromResult<LoginResponseDto?>(response);
        }
    }
}
