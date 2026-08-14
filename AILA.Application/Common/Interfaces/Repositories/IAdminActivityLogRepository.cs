using AILA.Domain.Entities;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IAdminActivityLogRepository
        : IGenericRepository<AdminActivityLog>
    {
        Task<IReadOnlyList<AdminActivityLog>> GetLogsAsync(
            DateTime? fromDate,
            DateTime? toDate);

        Task<IReadOnlyList<AdminActivityLog>> GetLogsWithActionAsync(
            DateTime? fromDate,
            DateTime? toDate,
            AdminAction? action);
    }
}
