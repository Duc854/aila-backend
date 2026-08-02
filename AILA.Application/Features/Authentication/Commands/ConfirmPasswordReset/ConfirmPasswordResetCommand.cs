using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.ConfirmPasswordReset
{
    /// <summary>
    /// Endpoint C — đặt password mới bằng reset token đã cấp ở bước verify.
    /// </summary>
    public record ConfirmPasswordResetCommand(
        string ResetToken,
        string NewPassword,
        string ConfirmPassword)
        : IRequest<ResponseDto<object>>;
}
