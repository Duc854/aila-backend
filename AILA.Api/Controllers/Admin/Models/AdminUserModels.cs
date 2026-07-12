using AILA.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AILA.Api.Controllers.Admin
{
    public enum AccountStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public class ManageUserRequest
    {
        [Required(ErrorMessage = "User Id là bắt buộc.")]
        public Guid? UserId { get; init; }

        [StringLength(100, ErrorMessage = "Search Keyword tối đa 100 ký tự.")]
        public string? SearchKeyword { get; init; }

        [Required(ErrorMessage = "Account Status là bắt buộc.")]
        public AccountStatus? AccountStatus { get; init; }

        [Required(ErrorMessage = "User Role là bắt buộc.")]
        public UserRole? UserRole { get; init; }
    }

    public class CreateExpertAccountRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        public string? FullName { get; init; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; init; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        public string? Password { get; init; }
    }

    public class UpdateUserStatusRequest
    {
        [Required(ErrorMessage = "Account Status là bắt buộc.")]
        public AccountStatus? AccountStatus { get; init; }
    }

    public record UserListItemResponse(Guid Id, string Email, string FullName, UserRole Role, AccountStatus AccountStatus);

    public record UserManagementResponse(Guid Id, string Email, string FullName, UserRole Role, AccountStatus AccountStatus);
}
