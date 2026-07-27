using AILA.Domain.Enums;

namespace AILA.Application.Features.Tags.Dtos;

public record VerifyTagRequest(
    VerifyDecision Decision,
    string? RejectionReason
);