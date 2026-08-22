namespace Asset.Application.Interfaces.Comman
{
    public interface ICheckRepository<T>
    {
        Task<bool> IsNameExistsAsync(string name, int? exceptId, CancellationToken ct);
        Task<bool> IsCodeExistsAsync(string code, int? exceptId, CancellationToken ct);

    }
}
