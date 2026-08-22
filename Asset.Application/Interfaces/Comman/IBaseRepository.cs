namespace Asset.Application.Interfaces.Comman
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct);
        Task AddAsync(T entity, CancellationToken ct);
        void UpdateAsync(T entity);
    }
}