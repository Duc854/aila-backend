using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Dtos;

public class TagDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public Guid? CreatedById { get; init; }

    public string Source { get; init; } = string.Empty;

    public int UsageCount { get; init; }
}