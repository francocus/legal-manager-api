using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class GenericRepository<T>(LegalManagerDbContext context) : IRepository<T> where T : class, IEntity
    {
        protected readonly LegalManagerDbContext _context = context;
        protected readonly DbSet<T> _dbSet = context.Set<T>();

        public virtual void Add(T entity) => _dbSet.Add(entity);

        public virtual void Update(T entity) => _dbSet.Update(entity);

        public virtual void Delete(T entity) => _dbSet.Remove(entity);

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _dbSet.Where(e => e.Active).ToListAsync(cancellationToken);

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.Active, cancellationToken);

        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
