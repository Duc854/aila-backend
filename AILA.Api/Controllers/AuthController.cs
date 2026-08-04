using AILA.Application.Features.Authentication.Commands;
using AILA.Application.Features.Authentication.Commands.AdminLogin;
using AILA.Application.Features.Authentication.Commands.ConfirmPasswordReset;
using AILA.Application.Features.Authentication.Commands.ExpertLogin;
using AILA.Application.Features.Authentication.Commands.GoogleCallback;
using AILA.Application.Features.Authentication.Commands.RefreshToken;
using AILA.Application.Features.Authentication.Commands.Register;
using AILA.Application.Features.Authentication.Commands.RequestPasswordReset;
using AILA.Application.Features.Authentication.Commands.VerifyPasswordResetOtp;
using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ISender sender, ILogger<AuthController> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequestDto request)
        {
            var command = new AdminLoginCommand(request.Email, request.Password);
            var result = await _sender.Send(command);

            if (result is null)
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "INVALID_CREDENTIALS",
                        "Tên tài khoản hoặc mật khẩu Admin không đúng."));

            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ResponseDto<LoginResponseDto>.SuccessResult(result));
        }

        [HttpPost("expert/login")]
        public async Task<IActionResult> ExpertLogin([FromBody] ExpertLoginRequestDto request)
        {
            var command = new ExpertLoginCommand(request.Email, request.Password);
            var result = await _sender.Send(command);

            if (result is null)
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "INVALID_CREDENTIALS",
                        "Email hoặc mật khẩu không đúng, hoặc tài khoản không có quyền Expert."));

            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ResponseDto<LoginResponseDto>.SuccessResult(result));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await _sender.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(Register), result);
        }

        #region UC-08: Reset Password

        /// <summary>
        /// Bước 1 — xin OTP đặt lại mật khẩu.
        /// Luôn trả về cùng một nội dung dù email có tồn tại hay không (chống user enumeration).
        /// </summary>
        [HttpPost("password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset(
            [FromBody] RequestPasswordResetRequestDto request,
            CancellationToken cancellationToken)
        {
            var command = new RequestPasswordResetCommand(request.Email, GetClientIpAddress());
            var result = await _sender.Send(command, cancellationToken);

            return result.Success ? Ok(result) : MapPasswordResetError(result.ErrorCode, result);
        }

        /// <summary>
        /// Bước 2 — xác thực OTP, đổi lấy reset token dùng một lần.
        /// </summary>
        [HttpPost("password-reset/verify")]
        public async Task<IActionResult> VerifyPasswordResetOtp(
            [FromBody] VerifyPasswordResetOtpRequestDto request,
            CancellationToken cancellationToken)
        {
            var command = new VerifyPasswordResetOtpCommand(request.Email, request.Otp);
            var result = await _sender.Send(command, cancellationToken);

            return result.Success ? Ok(result) : MapPasswordResetError(result.ErrorCode, result);
        }

        /// <summary>
        /// Bước 3 — đặt mật khẩu mới bằng reset token.
        /// </summary>
        [HttpPost("password-reset/confirm")]
        public async Task<IActionResult> ConfirmPasswordReset(
            [FromBody] ConfirmPasswordResetRequestDto request,
            CancellationToken cancellationToken)
        {
            var command = new ConfirmPasswordResetCommand(
                request.ResetToken,
                request.NewPassword,
                request.ConfirmPassword);

            var result = await _sender.Send(command, cancellationToken);

            return result.Success ? Ok(result) : MapPasswordResetError(result.ErrorCode, result);
        }

        private IActionResult MapPasswordResetError(string? errorCode, object body) => errorCode switch
        {
            PasswordResetErrorCodes.RateLimitExceeded  => StatusCode(StatusCodes.Status429TooManyRequests, body),
            PasswordResetErrorCodes.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, body),
            _                                          => BadRequest(body)
        };

        /// <summary>
        /// IP dùng cho rate limit. Ưu tiên X-Forwarded-For khi API chạy sau reverse proxy,
        /// nếu không thì lấy IP kết nối trực tiếp.
        /// </summary>
        private string? GetClientIpAddress()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                var firstHop = forwarded.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(firstHop))
                    return firstHop;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        #endregion

        private string GetCurrentGoogleRedirectUri()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            return $"{baseUrl}/api/auth/google/callback";
        }

        [HttpGet("google/url")]
        public IActionResult GetGoogleAuthorizationUrl([FromQuery] string? returnUrl, [FromQuery] bool redirect = false)
        {
            var clientId = Request.HttpContext.RequestServices.GetService<IOptions<GoogleSettings>>()?.Value?.ClientId;
            var redirectUri = GetCurrentGoogleRedirectUri();

            _logger.LogInformation("Generating Google auth URL. returnUrl={ReturnUrl}, redirect={Redirect}, redirectUri={RedirectUri}", returnUrl, redirect, redirectUri);

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
                return BadRequest(ResponseDto<object>.FailResult("GOOGLE_SETTINGS_NOT_CONFIGURED", "GoogleSettings chưa được cấu hình đúng."));

            var state = string.IsNullOrWhiteSpace(returnUrl)
                ? string.Empty
                : Uri.EscapeDataString(returnUrl);

            var scope = Uri.EscapeDataString("openid email profile");
            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={scope}&access_type=offline&prompt=select_account";
            if (!string.IsNullOrWhiteSpace(state))
            {
                authUrl += $"&state={state}";
            }

            if (redirect)
            {
                _logger.LogInformation("Redirecting browser to Google auth endpoint.");
                return Redirect(authUrl);
            }

            return Ok(new { AuthorizationUrl = authUrl });
        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? state)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_REQUEST", "Authorization code is missing."));

            _logger.LogInformation("Google callback received. codeLength={CodeLength}, statePresent={StatePresent}", code.Length, !string.IsNullOrWhiteSpace(state));

            var result = await _sender.Send(new GoogleCallbackCommand { AuthorizationCode = code, RedirectUri = GetCurrentGoogleRedirectUri() });
            if (!result.Success)
            {
                _logger.LogWarning("Google callback failed. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}", result.ErrorCode, result.ErrorMessage);
                return BadRequest(result);
            }

            _logger.LogInformation("Google callback succeeded for external user. userEmail={Email}", result.Data?.Email);
            SetRefreshTokenCookie(result.Data!.RefreshToken);

            if (!string.IsNullOrWhiteSpace(state))
            {
                var returnUrl = Uri.UnescapeDataString(state);
                var fragment = $"accessToken={Uri.EscapeDataString(result.Data!.AccessToken)}&refreshToken={Uri.EscapeDataString(result.Data.RefreshToken)}";
                return Redirect($"{returnUrl}#{fragment}");
            }

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequestDto? request)
        {
            var refreshToken = request?.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                refreshToken = Request.Cookies["refreshToken"];
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(ResponseDto<object>.FailResult("INVALID_REFRESH_TOKEN", "Refresh token không hợp lệ."));
            }

            var result = await _sender.Send(new RefreshTokenCommand(refreshToken));
            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(ResponseDto<LoginResponseDto>.SuccessResult(result));
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var isHttps = Request.IsHttps || Request.Headers["X-Forwarded-Proto"].ToString().Equals("https", StringComparison.OrdinalIgnoreCase);
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                MaxAge = TimeSpan.FromDays(7)
            };

            Response.Cookies.Append("refreshToken", refreshToken, options);
        }
    }
}
