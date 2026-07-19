using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories.AI
{
    public interface IPracticeChatService
    {
        IAsyncEnumerable<string> StreamResponseAsync(
            Guid attemptId, string prompt, CancellationToken ct);
    }
}
