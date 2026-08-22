namespace Asset.Application.Interfaces.Comman
{
    public interface IActiveRepository<T>
    {
        Task<bool> ExistsActiveAsync(int id, CancellationToken ct);
    }
}
