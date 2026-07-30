using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

namespace UserManagementPoC.Shared.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;
        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();

        }
        public async Task<T?> GetByIdAsync(string id) => await _dbSet.FindAsync(id);
        public async Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? queryBuilder = null)
        {
            var query = _dbSet.AsQueryable();
            if (queryBuilder != null) query = queryBuilder(query);
            return await query.ToListAsync();

        }
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? queryBuilder = null)
        {
            var query = _dbSet.Where(predicate);
            if (queryBuilder != null) query = queryBuilder(query);
            return await query.ToListAsync();

        }
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? queryBuilder = null)
        {
            var query = _dbSet.Where(predicate);
            if (queryBuilder != null) query = queryBuilder(query);
            return await query.FirstOrDefaultAsync();

        }
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => await _dbSet.AnyAsync(predicate);
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
            => predicate is null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        public async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);
        public void Update(T entity) => _dbSet.Update(entity);
        public void Delete(T entity) => _dbSet.Remove(entity);
        public void DeleteRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    }
}