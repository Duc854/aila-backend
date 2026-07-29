using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.RequestPasswordReset
{
    /// <summary>
    /// Endpoint A — sinh & gửi OTP reset password.
    /// </summary>
    /// <param name="Email">Email người dùng nhập (chưa chuẩn hoá).</param>
    /// <param name="IpAddress">IP của caller, dùng cho rate limit theo IP.</param>
    public record RequestPasswordResetCommand(string Email, string? IpAddress)
        : IRequest<ResponseDto<RequestPasswordResetResponseDto>>;
}
