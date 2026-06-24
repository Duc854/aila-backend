using AILA.Application.Common.Dtos;
using AILA.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Đăng nhập Admin — username/password đọc từ appsettings.Development.json
        /// (AdminCredentials:Username / AdminCredentials:Password).
        /// Không cần tra cứu database.
        /// </summary>
        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequestDto request)
        {
            var command = new AdminLoginCommand(request.Username, request.Password);
            var result  = await _mediator.Send(command);

            if (result is null)
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "INVALID_CREDENTIALS",
                        "Tên tài khoản hoặc mật khẩu Admin không đúng."));

            return Ok(ResponseDto<LoginResponseDto>.SuccessResult(result));
        }

        /// <summary>
        /// Đăng nhập Expert — email + password tra cứu trong database, xác minh bằng BCrypt.
        /// </summary>
        [HttpPost("expert/login")]
        public async Task<IActionResult> ExpertLogin([FromBody] ExpertLoginRequestDto request)
        {
            var command = new ExpertLoginCommand(request.Email, request.Password);
            var result  = await _mediator.Send(command);

            if (result is null)
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "INVALID_CREDENTIALS",
                        "Email hoặc mật khẩu không đúng, hoặc tài khoản không có quyền Expert."));

            return Ok(ResponseDto<LoginResponseDto>.SuccessResult(result));
        }
    }
}
