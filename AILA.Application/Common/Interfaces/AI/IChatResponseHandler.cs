// IChatResponseHandler.cs
using AILA.Application.Common.Dtos.Rag;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IChatResponseHandler
{
    Task<ChatResponseResult> GetResponseAsync(
        ChatHistoryDto chatHistory,
        CancellationToken cancellationToken = default);
}