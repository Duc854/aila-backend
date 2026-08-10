using AILA.Application.Features.Users.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Commands.CreateExpertAccount
{
    public record CreateExpertAccountCommand(
        string FullName,
        string Email,
        string Password
    ) : IRequest<ResponseDto<UserDetailDto>>;
}
