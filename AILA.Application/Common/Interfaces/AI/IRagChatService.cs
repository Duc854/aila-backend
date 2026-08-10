using AILA.Application.Common.Dtos.Rag;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IRagChatService
{
    Task<AskRagQuestionResponseDto> AskCourseQuestionAsync(Guid sessionId, Guid accountId, string question, CancellationToken cancellationToken = default);
}
