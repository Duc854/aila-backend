using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.GoogleLogin
{
    public class GoogleLoginCommand : IRequest<ResponseDto<LoginResponseDto>>
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, ResponseDto<LoginResponseDto>>
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

        public async Task<ResponseDto<LoginResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var payload = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);
            if (payload == null)
                return ResponseDto<LoginResponseDto>.FailResult("INVALID_GOOGLE_TOKEN", "Google Token không hợp lệ.");

            var user = await _unitOfWork.Users.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new User(payload.Email, payload.Name, UserRole.Learner, null);
                await _unitOfWork.Users.AddAsync(user);

                var learner = new Learner(user.Id);
                await _unitOfWork.Learners.AddAsync(learner);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else if (user.Role != UserRole.Learner)
            {
                return ResponseDto<LoginResponseDto>.FailResult("INVALID_ROLE", "Tài khoản không phải là Learner.");
            }

            if (user != null && !user.IsActive)
            {
                return ResponseDto<LoginResponseDto>.FailResult("ACCOUNT_BANNED", "Tài khoản của bạn đã bị khóa.");
            }

            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };

            return ResponseDto<LoginResponseDto>.SuccessResult(response);
        }
    }
}
