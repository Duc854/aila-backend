using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections;

namespace AILA.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction?        _currentTransaction;
        private Hashtable?                    _repositories;
        private bool                          _disposed;

        public ICourseRepository           Courses            { get; private set; }
        public ILearningProgressRepository LearningProgresses { get; private set; }
        public IUserRepository             Users              { get; private set; }
        public INotificationRepository     Notifications      { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context           = context;
            Courses            = new CourseRepository(_context);
            LearningProgresses = new LearningProgressRepository(_context);
            Users              = new UserRepository(_context);
            Notifications      = new NotificationRepository(_context);
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
            => await _context.SaveChangesAsync(cancellationToken);

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
                    await _currentTransaction.CommitAsync(cancellationToken);
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
                    await _currentTransaction.RollbackAsync(cancellationToken);
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
