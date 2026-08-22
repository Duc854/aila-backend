namespace AILA.Application.Features.VideoMaterials.Dtos;

public sealed record CreateVideoMaterialRequest(
    Guid ModuleId,
    string Title,
    string VideoUrl,
    int DurationSeconds,
    string? Content
);
