using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ICourseChatMessageRepository
    {
        Task AddMessageAsync(CourseChatMessage message, CancellationToken cancellationToken = default);
        Task<List<CourseChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<List<CourseChatMessage>> GetRecentMessagesAsync(Guid sessionId, int count = 6, CancellationToken cancellationToken = default);

    }
}
