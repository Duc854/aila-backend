using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.LearnerLogin
{
    public class LearnerLoginCommand : IRequest<ResponseDto<LoginResponseDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LearnerLoginCommandHandler : IRequestHandler<LearnerLoginCommand, ResponseDto<LoginResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenProvider _tokenProvider;

        public LearnerLoginCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenProvider tokenProvider)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenProvider = tokenProvider;
        }

        public async Task<ResponseDto<LoginResponseDto>> Handle(LearnerLoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null || user.Role != UserRole.Learner)
                return ResponseDto<LoginResponseDto>.FailResult("INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.");

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash!))
                return ResponseDto<LoginResponseDto>.FailResult("INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.");

            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();
            var refreshTokenHash = _tokenProvider.HashToken(refreshToken);

            var userToken = new UserToken(
                user.Id,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(7));

            _unitOfWork.UserTokens.Add(userToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
