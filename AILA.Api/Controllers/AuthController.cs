using AILA.Application.Features.Authentication.Commands.AdminLogin;
using AILA.Application.Features.Authentication.Commands.ExpertLogin;
using AILA.Application.Features.Authentication.Commands.GoogleLogin;
using AILA.Application.Features.Authentication.Commands.Register;
using AILA.Application.Features.Authentication.Dtos;
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
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequestDto request)
        {
            var command = new AdminLoginCommand(request.Username, request.Password);
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

        [HttpPost("learner/google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {
            var result = await _sender.Send(command);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }
    }
}
