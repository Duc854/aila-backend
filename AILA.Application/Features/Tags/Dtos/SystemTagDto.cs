using System;

namespace AILA.Application.Features.Tags.Dtos;

public class SystemTagDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public string Source { get; init; } = string.Empty;

    public int UsageCount { get; init; }

    public DateTime CreatedAt { get; init; }
}
