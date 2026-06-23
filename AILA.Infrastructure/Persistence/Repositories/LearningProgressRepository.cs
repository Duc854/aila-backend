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
    public class LearningProgressRepository
        : GenericRepository<LearningProgress>,
          ILearningProgressRepository
    {
        private readonly ApplicationDbContext _context;

        public LearningProgressRepository(
            ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }

        public async Task<List<Guid>>
            GetCompletedMaterialIdsAsync(
                Guid courseId,
                Guid userId)
        {
            return await _context
                .LearningProgresses
                .Where(x =>
                    x.Enrollment.CourseId == courseId &&
                    x.Enrollment.LearnerId == userId &&
                    x.IsCompleted)
                .Select(x => x.MaterialId)
                .ToListAsync();
        }

        public async Task<Guid?>
            GetCurrentMaterialIdAsync(
                Guid courseId,
                Guid userId)
        {
            return await _context
                .LearningProgresses
                .Where(x =>
                    x.Enrollment.CourseId == courseId &&
                    x.Enrollment.LearnerId == userId)
                .OrderByDescending(
                    x => x.LastAccessedAt)
                .Select(x => x.MaterialId)
                .FirstOrDefaultAsync();
        }
    }
}
