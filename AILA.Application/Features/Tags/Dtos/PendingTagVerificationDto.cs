using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AILA.Domain.Enums;

namespace AILA.Application.Features.Tags.Dtos;

public class PendingTagVerificationDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public Guid? CreatedById { get; init; }

    public string? SubmittedBy { get; init; }

    public Domain.Enums.TagPublishRequestStatus? RequestStatus { get; init; }

    public DateTime? SubmittedAt { get; init; }
}
