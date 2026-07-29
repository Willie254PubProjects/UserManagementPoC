using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Storage;

namespace UserManagementPoC.Shared.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private Dictionary<Type, object>? _repositories;
        private IDbContextTransaction? _transaction;
        public UnitOfWork(DbContext context) => _context = context;
        public IRepository<T> Repository<T>() where T : class
        {
            _repositories ??= [];
            var type = typeof(T);
            if (!_repositories.ContainsKey(type)) _repositories[type] = new Repository<T>(_context);
            return (IRepository<T>)_repositories[type];

        }
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
        public async Task BeginTransactionAsync() => _transaction = await _context.Database.BeginTransactionAsync();
        public async Task CommitTransactionAsync()
        {
            await _transaction!.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;

        }
        public async Task RollbackTransactionAsync()
        {
            await _transaction!.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;

        }
        public void Dispose() => _transaction?.Dispose();

    }
}