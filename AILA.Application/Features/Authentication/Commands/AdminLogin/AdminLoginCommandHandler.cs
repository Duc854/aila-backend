using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
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
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenProvider _tokenProvider;

        public AdminLoginCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider)
        {
            _uow = uow;
            _passwordHasher = passwordHasher;
            _tokenProvider = tokenProvider;
        }

        public async Task<LoginResponseDto?> Handle(
            AdminLoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _uow.Users.GetByEmailAsync(request.Email);

            if (user is null || user.PasswordHash is null)
                throw new UnauthorizedAccessException("Sai email hoặc mật khẩu.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

            bool isValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isValid)
                throw new UnauthorizedAccessException("Sai email hoặc mật khẩu.");

            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = "Administrator",
                Email = "adminEmail",
            };

            return response;
        }
    }
}
