// AILA.Application.Common.Interfaces.AI/IPromptValidationService.cs
using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.AI;

public interface IPromptValidationService
{
    /// <summary>
    /// Validate prompt đầu vào
    /// </summary>
    Task<(bool IsValid, string? ViolationReason, string? PolicyName)> ValidateAsync(
        string prompt,
        PracticeAttempt attempt,
        CancellationToken cancellationToken = default);
}
