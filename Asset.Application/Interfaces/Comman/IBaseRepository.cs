namespace Asset.Application.Interfaces.Comman
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct);
        void Add(T entity);
        void Update(T entity);
    }
}