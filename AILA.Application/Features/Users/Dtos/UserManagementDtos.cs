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
        [Required(ErrorMessage = "User Id là bắt buộc.")]
        public Guid? UserId { get; init; }

        [StringLength(100, ErrorMessage = "Search Keyword tối đa 100 ký tự.")]
        public string? SearchKeyword { get; init; }

        [Required(ErrorMessage = "Account Status là bắt buộc.")]
        public AccountStatus? AccountStatus { get; init; }

        [Required(ErrorMessage = "User Role là bắt buộc.")]
        public UserRole? UserRole { get; init; }
    }
}
