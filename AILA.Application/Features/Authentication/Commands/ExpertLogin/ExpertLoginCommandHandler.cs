using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System.Data;

namespace AILA.Application.Features.Authentication.Commands.ExpertLogin
{
    public class ExpertLoginCommandHandler
        : IRequestHandler<ExpertLoginCommand, ResponseDto<LoginResponseDto?>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenProvider _tokenProvider;

        public ExpertLoginCommandHandler(
            IUnitOfWork uow,
            IPasswordHasher passwordHasher,
            ITokenProvider tokenProvider)
        {
            _uow = uow;
            _passwordHasher = passwordHasher;
            _tokenProvider = tokenProvider;
        }

        public async Task<ResponseDto<LoginResponseDto>> Handle(
            ExpertLoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _uow.Users.GetByEmailAsync(request.Email);

            // Account does not exist / wrong role / no password
            if (user is null
                || user.Role != UserRole.Expert)
            {
                return ResponseDto<LoginResponseDto>.FailResult(
                    "ACCESS_DENIED",
                    "Chỉ tài khoản chuyên gia mới có thể đăng nhập tại đây");
            }

            // Account is inactive
            if (!user.IsActive)
            {
                return ResponseDto<LoginResponseDto>.FailResult(
                    "ACCOUNT_INACTIVE",
                    "Tài khoản đã bị vô hiệu hóa");
            }

            // Wrong password
            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return ResponseDto<LoginResponseDto>.FailResult(
                    "CREDENTIAL_FAILED",
                    "Tài khoản hoặc mật khẩu không chính xác");
            }

            // Generate tokens
            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();
            var refreshTokenHash = _tokenProvider.HashToken(refreshToken);

            // Store refresh token hash
            var userToken = new UserToken(
                user.Id,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(7));

            _uow.UserTokens.Add(userToken);

            await _uow.SaveChangesAsync(cancellationToken);

            // Build response
            var result = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };

            return ResponseDto<LoginResponseDto>.SuccessResult(result);
        }
    }
}
