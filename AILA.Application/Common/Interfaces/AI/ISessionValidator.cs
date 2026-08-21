using AILA.Application.Common.Dtos.Rag;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface ISessionValidator
{
    Task<SessionValidationResult> ValidateAsync(
        Guid sessionId,
        Guid accountId,
        string question,
        int maxHistoryMessages = 6,
        CancellationToken cancellationToken = default);
}