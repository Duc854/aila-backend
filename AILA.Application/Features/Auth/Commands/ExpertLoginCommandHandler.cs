using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.Auth.Commands
{
    public class ExpertLoginCommandHandler
        : IRequestHandler<ExpertLoginCommand, LoginResponseDto?>
    {
        private readonly IUnitOfWork     _uow;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenProvider  _tokenProvider;

        public ExpertLoginCommandHandler(
            IUnitOfWork     uow,
            IPasswordHasher passwordHasher,
            ITokenProvider  tokenProvider)
        {
            _uow            = uow;
            _passwordHasher = passwordHasher;
            _tokenProvider  = tokenProvider;
        }

        public async Task<LoginResponseDto?> Handle(
            ExpertLoginCommand request,
            CancellationToken  cancellationToken)
        {
            // 1. Tìm User theo email trong DB
            var user = await _uow.Users.GetByEmailAsync(request.Email);

            // 2. Kiểm tra user tồn tại, đúng role Expert, đang Active
            if (user is null
                || user.Role != UserRole.Expert
                || !user.IsActive
                || user.PasswordHash is null)
                return null;

            // 3. Xác minh mật khẩu bằng BCrypt
            var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
                return null;

            // 4. Phát hành token
            var accessToken  = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            return new LoginResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                Role         = user.Role.ToString(),
                UserId       = user.Id,
                FullName     = user.FullName,
                Email        = user.Email
            };
        }
    }
}
