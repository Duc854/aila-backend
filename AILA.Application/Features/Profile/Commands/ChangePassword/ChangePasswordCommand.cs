using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.ChangePassword
{
    public record ChangePasswordCommand(
        Guid UserId,
        string? CurrentPassword,
        string NewPassword
    ) : IRequest<ResponseDto<object>>;
}
