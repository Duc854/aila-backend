namespace AILA.Application.Features.Users.Dtos
{
    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; init; }  // Dùng bool thay vì AccountStatus enum
    }
}
