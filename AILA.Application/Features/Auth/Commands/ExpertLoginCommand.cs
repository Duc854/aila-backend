using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Auth.Commands
{
    /// <summary>
    /// Command: Expert đăng nhập bằng Email + Password lưu trong database.
    /// </summary>
    public record ExpertLoginCommand(string Email, string Password)
        : IRequest<LoginResponseDto?>;
}
