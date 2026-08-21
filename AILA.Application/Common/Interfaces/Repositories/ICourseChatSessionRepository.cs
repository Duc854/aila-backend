using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ICourseChatSessionRepository
    {
        Task<CourseChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<List<CourseChatSession>> GetSessionsByAccountAndCourseAsync(Guid accountId, Guid courseId, CancellationToken cancellationToken = default);
        Task AddSessionAsync(CourseChatSession session, CancellationToken cancellationToken = default);

    }
}
