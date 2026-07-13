using AILA.Domain.Enums;

namespace AILA.Application.Features.Tags.Dtos;

public record TagPublishRequestStatus(
    VerifyDecision Decision,
    string? RejectionReason
);