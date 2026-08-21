using AILA.Application.Common.Dtos.Rag;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IMessagePersistence
{
    Task SaveViolationRecordAsync(
        Guid accountId,
        string violationType,
        string policyName,
        string reason,
        string prompt,
        CancellationToken cancellationToken = default);

    Task SaveMessagesAsync(
        Guid sessionId,
        string question,
        string answer,
        List<RagCitationDto> citations,
        int promptTokens,
        int completionTokens,
        CancellationToken cancellationToken = default);
}