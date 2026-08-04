using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.Authentication.Commands.ExpertLogin
{
    public class ExpertLoginCommandHandler
        : IRequestHandler<ExpertLoginCommand, LoginResponseDto?>
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

        public async Task<LoginResponseDto?> Handle(
            ExpertLoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _uow.Users.GetByEmailAsync(request.Email);

            if (user is null
                || user.Role != UserRole.Expert
                || !user.IsActive
                || user.PasswordHash is null)
                return null;

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
                return null;

            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();
            var refreshTokenHash = _tokenProvider.HashToken(refreshToken);

            var userToken = new UserToken(
                user.Id,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(7));

            _uow.UserTokens.Add(userToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };
        }
    }
}
