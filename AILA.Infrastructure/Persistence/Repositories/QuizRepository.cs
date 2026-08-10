using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class QuizRepository : GenericRepository<QuizMaterial>, IQuizRepository
    {
        public QuizRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<QuizMaterial?> GetQuizForLearningAsync(Guid courseId, Guid materialId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizMaterials
                .AsNoTracking()
                .Include(q => q.Questions)
                    .ThenInclude(question => question.AnswerOptions)
                .FirstOrDefaultAsync(
                    q => q.MaterialId == materialId
                         && q.Material.Module.CourseId == courseId,
                    cancellationToken);
        }

        public async Task<QuizAttempt?> GetInProgressAttemptAsync(Guid enrollmentId, Guid quizMaterialId, CancellationToken cancellationToken = default)
        {
            // Lấy lượt InProgress MỚI NHẤT: sau khi mở lượt mới (vì lượt cũ hết hạn),
            // luôn trả về lượt mới nhất, không vô tình bốc lại lượt cũ đã hết giờ.
            return await _context.QuizAttempts
                .Where(a => a.EnrollmentId == enrollmentId
                            && a.QuizMaterialId == quizMaterialId
                            && a.Status == QuizAttemptStatus.InProgress)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<QuizAttempt?> GetAttemptWithAnswersAsync(Guid attemptId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);
        }

        public async Task<QuizAttempt?> GetLatestSubmittedAttemptAsync(Guid enrollmentId, Guid quizMaterialId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Answers)
                .Where(a => a.EnrollmentId == enrollmentId
                            && a.QuizMaterialId == quizMaterialId
                            && a.Status == QuizAttemptStatus.Submitted)
                .OrderByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<QuizAttempt>> GetSubmittedAttemptsByLearnerAsync(Guid learnerId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.QuizMaterial)
                    .ThenInclude(q => q.Material)
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Course)
                .Where(a => a.Enrollment.LearnerId == learnerId
                            && a.Status == QuizAttemptStatus.Submitted)
                .OrderByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.StartedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<QuizAttempt> Items, int TotalCount)> GetPagedSubmittedAttemptsByLearnerAsync(
            Guid learnerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.QuizMaterial)
                    .ThenInclude(q => q.Material)
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Course)
                .Where(a => a.Enrollment.LearnerId == learnerId
                            && a.Status == QuizAttemptStatus.Submitted);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.StartedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default)
        {
            await _context.QuizAttempts.AddAsync(attempt, cancellationToken);
        }

        public async Task AddAnswerAsync(QuizAnswer answer, CancellationToken cancellationToken = default)
        {
            await _context.QuizAnswers.AddAsync(answer, cancellationToken);
        }

        public async Task<int> GetQuizAttemptsCountInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken = default)
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
                .CountAsync(cancellationToken);
        }
    }
}
