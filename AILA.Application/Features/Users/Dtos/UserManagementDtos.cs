using AILA.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AILA.Application.Features.Users.Dtos
{
    public enum AccountStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public class ManageUserRequest
    {
        [Required(ErrorMessage = "Mã người dùng là bắt buộc.")]
        public Guid? UserId { get; init; }

        [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm tối đa 100 ký tự.")]
        public string? SearchKeyword { get; init; }

        [Required(ErrorMessage = "Trạng thái tài khoản là bắt buộc.")]
        public AccountStatus? AccountStatus { get; init; }

        [Required(ErrorMessage = "Vai trò người dùng là bắt buộc.")]
        public UserRole? UserRole { get; init; }
    }
}
