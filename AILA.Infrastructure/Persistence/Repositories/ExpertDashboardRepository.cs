using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class ExpertDashboardRepository : IExpertDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpertDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Course>> GetPublishedCoursesByExpertAsync(
            Guid expertId, 
            CancellationToken ct = default)
        {
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.ExpertId == expertId && c.IsPublished)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<List<Enrollment>> GetEnrollmentsInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default)
        {
            if (courseIds == null || courseIds.Count == 0)
                return new List<Enrollment>();

            return await _context.Enrollments
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId) 
                         && e.EnrolledAt >= fromDate 
                         && e.EnrolledAt <= toDate)
                .ToListAsync(ct);
        }

        public async Task<int> GetQuizAttemptsCountInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default)
        {
            if (courseIds == null || courseIds.Count == 0)
                return 0;

            return await _context.QuizAttempts
                .AsNoTracking()
                .Where(qa => _context.Enrollments
                                .Where(e => courseIds.Contains(e.CourseId))
                                .Select(e => e.Id)
                                .Contains(qa.EnrollmentId)
                          && qa.StartedAt >= fromDate
                          && qa.StartedAt <= toDate)
                .CountAsync(ct);
        }

        public async Task<int> GetPracticeAttemptsCountInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default)
        {
            if (courseIds == null || courseIds.Count == 0)
                return 0;

            return await _context.PracticeAttempts
                .AsNoTracking()
                .Where(pa => _context.Enrollments
                                .Where(e => courseIds.Contains(e.CourseId))
                                .Select(e => e.Id)
                                .Contains(pa.EnrollmentId)
                          && pa.CreatedAt >= fromDate
                          && pa.CreatedAt <= toDate)
                .CountAsync(ct);
        }
    }
}
