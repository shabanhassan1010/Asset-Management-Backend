using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;

namespace Asset.Application.Interfaces.IRepository
{
    public interface ICategoryRepository : IBaseRepository<Category>, IActiveRepository<Category>
    {
        Task<List<GetCategoryListResponse>> GetAllProjectedAsync(CancellationToken ct);
        Task<bool> CategoryNameExistsAsync(string name, int? exceptId, CancellationToken ct);
        Task<bool> HasAssetsAsync(int id, CancellationToken ct);
        void Remove(Category entity);
    }
}
