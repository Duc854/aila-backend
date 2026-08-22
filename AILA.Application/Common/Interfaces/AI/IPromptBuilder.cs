using AILA.Application.Common.Dtos.Rag;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IPromptBuilder
{
    Task<ChatHistoryDto> BuildAsync(
        string question,
        VectorSearchResult searchResult,
        List<CourseChatMessageDto> history,
        CancellationToken cancellationToken = default);
}