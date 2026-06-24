namespace AILA.Application.Features.Profile.Dtos
{
    public record ExpertInfoDto(
        string? Bio,
        string? Specialty,
        int YearsOfExperience
    );

    public record ExpertProfileDto(
        Guid Id,
        string FullName,
        string Email,
        string? AvatarUrl,
        string Role,
        ExpertInfoDto Expert
    );
}
