using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Auth.DTOs;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Auth.Commands
{
    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponse>
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenProvider _tokenProvider;

        public GoogleLoginCommandHandler(IGoogleAuthService googleAuthService, IUnitOfWork unitOfWork, ITokenProvider tokenProvider)
        {
            _googleAuthService = googleAuthService;
            _unitOfWork = unitOfWork;
            _tokenProvider = tokenProvider;
        }

        public async Task<AuthResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var payload = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);
            if (payload == null)
            {
                throw new UnauthorizedAccessException("Google Token không hợp lệ.");
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(payload.Email);

            if (user == null)
            {
                user = new User(
                    email: payload.Email,
                    fullName: payload.Name,
                    role: UserRole.Learner,
                    googleId: payload.Subject,
                    avatarUrl: payload.Picture
                );

                var learner = new Learner(user.Id);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _unitOfWork.Users.AddAsync(user);
                    await _unitOfWork.Learners.AddAsync(learner);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            else if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.LinkGoogleAccount(payload.Subject);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    AvatarUrl = user.AvatarUrl
                }
            };
        }
    }
}
