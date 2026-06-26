using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace AILA.Application.Features.Authentication.Commands.AdminLogin
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
            var adminUsername = _configuration["AdminCredentials:Username"];
            var adminPassword = _configuration["AdminCredentials:Password"];

            if (request.Username != adminUsername || request.Password != adminPassword)
                return Task.FromResult<LoginResponseDto?>(null);

            var adminVirtualUser = new User(
                email: "admin@aila.internal",
                fullName: "Administrator",
                role: UserRole.Admin,
                passwordHash: null);

            var accessToken = _tokenProvider.GenerateAccessToken(adminVirtualUser);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = UserRole.Admin.ToString(),
                UserId = adminVirtualUser.Id,
                FullName = "Administrator",
                Email = "admin@aila.internal"
            };

            return Task.FromResult<LoginResponseDto?>(response);
        }
    }
}
