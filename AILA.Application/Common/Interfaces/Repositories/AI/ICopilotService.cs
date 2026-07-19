using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories.AI
{
    public interface ICopilotQueryService
    {
        IAsyncEnumerable<CopilotStreamChunk> StreamAnswerAsync(
            Guid courseId, string question, IReadOnlyList<Guid> retrievedChunkIds, CancellationToken ct);
    }

    public record CopilotStreamChunk(string? Token, IReadOnlyList<Guid>? SourceChunkIds, bool IsFinal);
}
