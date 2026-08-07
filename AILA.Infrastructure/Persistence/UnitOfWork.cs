using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private Hashtable? _repositories;
        private bool _disposed;

        public ICourseRepository            Courses            { get; private set; }
        public ILearningProgressRepository  LearningProgresses { get; private set; }
        public ITagRepository               Tags               { get; private set; }
        public ICategoryRepository          Categories         { get; private set; }
        public IEnrollmentRepository        Enrollments        { get; private set; }
        public IUserRepository              Users              { get; private set; }
        public INotificationRepository      Notifications      { get; private set; }
        public IMaterialRepository          Materials          { get; private set; }
        public IBlogPostRepository          BlogPosts          { get; private set; }
        public ILearnerRepository           Learners           { get; private set; }
        public IExpertRepository            Experts            { get; private set; }
        public IModuleRepository Modules { get; private set; }
        public IQuizRepository Quizzes { get; private set; }
        public IContentReportRepository ContentReports { get; private set; }
        public IQuestionRepository Questions { get; private set; }
        public IAnswerOptionRepository AnswerOptions { get; private set; }
        public ISubscriptionPlanRepository SubscriptionPlans { get; private set; }
        public ISubscriptionRepository Subscriptions { get; private set; }
        public IAIPracticeMaterialRepository AIPracticeMaterials { get; private set; }
        public ICourseReviewRequestRepository CourseReviewRequests { get; private set; }
        public IUserTokenRepository UserTokens { get; private set; }
        public IExpertDashboardRepository ExpertDashboards { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context           = context;
            Courses            = new CourseRepository(_context);
            LearningProgresses = new LearningProgressRepository(_context);
            Tags               = new TagRepository(_context);
            Categories         = new CategoryRepository(_context);
            Enrollments        = new EnrollmentRepository(_context);
            Users              = new UserRepository(_context);
            Notifications      = new NotificationRepository(_context);
            Materials          = new MaterialRepository(_context);
            BlogPosts          = new BlogPostRepository(_context);
            Learners           = new LearnerRepository(_context);
            Experts            = new ExpertRepository(_context);
            Modules = new ModuleRepository(_context);
            Quizzes = new QuizRepository(_context);
            ContentReports = new ContentReportRepository(_context);
            Questions = new QuestionRepository(_context);
            AnswerOptions = new AnswerOptionRepository(_context);
            SubscriptionPlans = new SubscriptionPlanRepository(_context);
            Subscriptions = new SubscriptionRepository(_context);
            AIPracticeMaterials = new AIPracticeMaterialRepository(_context);
            CourseReviewRequests = new CourseReviewRequestRepository(_context);
            UserTokens = new UserTokenRepository(_context);
            ExpertDashboards = new ExpertDashboardRepository(_context);
        }
        public IGenericRepository<T> Repository<T>() where T : class
        {
            _repositories ??= new Hashtable();
            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType     = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(
                    repositoryType.MakeGenericType(typeof(T)), _context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Xử lý xung đột concurrency: reload DB values rồi retry (client wins)
                foreach (var entry in ex.Entries)
                {
                    var dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (dbValues == null)
                    {
                        entry.State = EntityState.Detached;
                    }
                    else
                    {
                        entry.OriginalValues.SetValues(dbValues);
                    }
                }

                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException { SqlState: "23505" } pgEx)
            {
                throw new DuplicateKeyException(pgEx.ConstraintName ?? string.Empty, ex);
            }
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null) return;

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                DisposeTransaction();
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync(cancellationToken);
                }
            }
            finally
            {
                DisposeTransaction();
            }
        }

        private void DisposeTransaction()
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }

        // Giải phóng tài nguyên DbContext khi Unit of Work bị hủy
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                    _currentTransaction?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
