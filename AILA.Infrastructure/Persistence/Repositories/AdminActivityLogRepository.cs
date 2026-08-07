using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class AdminActivityLogRepository
        : GenericRepository<AdminActivityLog>, IAdminActivityLogRepository
    {
        public AdminActivityLogRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }


        public async Task<IReadOnlyList<AdminActivityLog>> GetLogsAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            IQueryable<AdminActivityLog> query =
                _context.Set<AdminActivityLog>()
                    .AsNoTracking()
                    .Include(x => x.Admin);


            if (fromDate.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt >= fromDate.Value);
            }


            if (toDate.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt <= toDate.Value);
            }


            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
