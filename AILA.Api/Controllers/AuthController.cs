using AILA.Application.Features.Authentication.Commands.AdminLogin;
using AILA.Application.Features.Authentication.Commands.ExpertLogin;
using AILA.Application.Features.Authentication.Commands.GoogleCallback;
using AILA.Application.Features.Authentication.Commands.GoogleLogin;
using AILA.Application.Features.Authentication.Commands.Register;
using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

            if (!string.IsNullOrWhiteSpace(state))
            {
                var returnUrl = Uri.UnescapeDataString(state);
                var fragment = $"accessToken={Uri.EscapeDataString(result.Data!.AccessToken)}&refreshToken={Uri.EscapeDataString(result.Data.RefreshToken)}";
                return Redirect($"{returnUrl}#{fragment}");
            }

            return Ok(result);
        }
    }
}
