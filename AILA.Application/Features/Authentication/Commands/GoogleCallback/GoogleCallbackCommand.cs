using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.GoogleCallback;

public class GoogleCallbackCommand : IRequest<ResponseDto<LoginResponseDto>>
{
    public string AuthorizationCode { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

public class GoogleCallbackCommandHandler : IRequestHandler<GoogleCallbackCommand, ResponseDto<LoginResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<GoogleCallbackCommandHandler> _logger;

    public GoogleCallbackCommandHandler(
        IUnitOfWork unitOfWork,
        IGoogleAuthService googleAuthService,
        ITokenProvider tokenProvider,
        ILogger<GoogleCallbackCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _googleAuthService = googleAuthService;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<ResponseDto<LoginResponseDto>> Handle(GoogleCallbackCommand request, CancellationToken cancellationToken)
    {
        string? idToken;
        try
        {
            idToken = await _googleAuthService.ExchangeCodeForIdTokenAsync(request.AuthorizationCode, request.RedirectUri, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Google code exchange failed for redirectUri={RedirectUri}", request.RedirectUri);
            return ResponseDto<LoginResponseDto>.FailResult("INVALID_GOOGLE_CODE", $"Không thể đổi authorization code lấy token từ Google. {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(idToken))
        {
            _logger.LogWarning("Google code exchange returned empty idToken.");
            return ResponseDto<LoginResponseDto>.FailResult("INVALID_GOOGLE_CODE", "Không thể đổi authorization code lấy token từ Google.");
        }

        _logger.LogInformation("Google exchange returned idToken length={IdTokenLength}", idToken.Length);

        var payload = await _googleAuthService.VerifyGoogleTokenAsync(idToken);
        if (payload == null)
        {
            _logger.LogWarning("Google token verification failed after successful exchange.");
            return ResponseDto<LoginResponseDto>.FailResult("INVALID_GOOGLE_TOKEN", "Google token không hợp lệ.");
        }

        _logger.LogInformation("Google token verified: email={Email}, name={Name}", payload.Email, payload.Name);

        return await AuthenticateGoogleUserAsync(payload, cancellationToken);
    }

    private async Task<ResponseDto<LoginResponseDto>> AuthenticateGoogleUserAsync(GoogleTokenPayload payload, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(payload.Email);

        if (user == null)
        {
            user = new User(payload.Email, payload.Name, UserRole.Learner, null, payload.GoogleId, payload.Picture);
            var learner = new Learner(user.Id);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.Learners.AddAsync(learner);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (user.Role != UserRole.Learner)
        {
            return ResponseDto<LoginResponseDto>.FailResult("INVALID_ROLE", "Tài khoản không có quyền truy cập với vai trò Learner.");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(payload.GoogleId) && string.IsNullOrWhiteSpace(user.GoogleId))
            {
                user.LinkGoogleAccount(payload.GoogleId);
            }

            if (!string.IsNullOrWhiteSpace(payload.Picture))
            {
                user.UpdateProfile(payload.Name, payload.Picture);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var accessToken = _tokenProvider.GenerateAccessToken(user);
        var refreshToken = _tokenProvider.GenerateRefreshToken();

        return ResponseDto<LoginResponseDto>.SuccessResult(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Role = user.Role.ToString(),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
        });
    }
}
