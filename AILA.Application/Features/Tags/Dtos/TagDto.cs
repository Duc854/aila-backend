using AILA.Domain.Enums;

namespace AILA.Application.Features.Tags.Dtos;
public class TagDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public Guid? CreatedById { get; init; }

    public string? SubmittedBy { get; init; }

    public TagPublishRequestStatus? RequestStatus { get; init; }

    public DateTime? SubmittedAt { get; init; }

    public string? Note { get; init; }

    public string Source { get; init; } = string.Empty;

    public int UsageCount { get; init; }
}
