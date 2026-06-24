using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Auth.Commands
{
    /// <summary>
    /// Command: Admin đăng nhập bằng username/password đọc từ appsettings.
    /// Không tra cứu database — tài khoản admin được cấu hình tĩnh.
    /// </summary>
    public record AdminLoginCommand(string Username, string Password)
        : IRequest<LoginResponseDto?>;
}
