namespace LegalManager.Domain.Interfaces
{
    public interface IRepository<T> where T : class, IEntity
    {
        void Add(T entity);

        void Update(T entity);

        void Delete(T entity);

        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
