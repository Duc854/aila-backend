using AILA.Application.Common.Interfaces;
using MediatR;


namespace AILA.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommandHandler
        : IRequestHandler<LogoutCommand, bool>
    {

        private readonly IUnitOfWork _uow;

        private readonly ITokenProvider _tokenProvider;

        private readonly ITokenBlacklistService _blacklistService;

        public LogoutCommandHandler(
            IUnitOfWork uow,
            ITokenProvider tokenProvider,
            ITokenBlacklistService blacklistService)
        {
            _uow = uow;
            _tokenProvider = tokenProvider;
            _blacklistService = blacklistService;
        }

        public async Task<bool> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            /*
             * 1. Blacklist Access Token
             */
            var ttl =
                request.AccessTokenExpiredAt
                - DateTime.UtcNow;
            if (ttl > TimeSpan.Zero)
            {
                await _blacklistService
                    .BlacklistAsync(
                        request.Jti,
                        ttl);
            }
            /*
             * 2. Revoke Refresh Token
             */
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var refreshHash =
                    _tokenProvider
                        .HashToken(
                            request.RefreshToken);
                var userToken =
                    await _uow.UserTokens
                        .GetByRefreshTokenHashAsync(
                            refreshHash);
                if (userToken != null)
                {
                    userToken.Revoke();
                }
            }
            await _uow.SaveChangesAsync(
                cancellationToken);
            return true;
        }
    }
}