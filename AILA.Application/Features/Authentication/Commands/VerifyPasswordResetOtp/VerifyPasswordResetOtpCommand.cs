using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.VerifyPasswordResetOtp
{
    /// <summary>
    /// Endpoint B — xác thực OTP, đổi lấy reset token dùng một lần.
    /// </summary>
    public record VerifyPasswordResetOtpCommand(string Email, string Otp)
        : IRequest<ResponseDto<VerifyPasswordResetOtpResponseDto>>;
}
