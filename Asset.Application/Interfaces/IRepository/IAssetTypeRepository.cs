#region
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;
using System.Linq.Expressions;
#endregion
namespace Asset.Application.Interfaces.IRepository
{
    public interface IAssetTypeRepository : IBaseRepository<AssetType>
    {
        Task<IReadOnlyList<AssetType>> GetAllAsync(CancellationToken cancellationToken);
        Task<(List<GetAssetTypeListQueryResponse> Items, int TotalCount)> GetPagedAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken ct);
        Task<bool> AnyAsync(Expression<Func<AssetType, bool>> predicate, CancellationToken cancellationToken);
        Task<bool> AssetTypeNameExistsAsync(string name, int? exceptId, CancellationToken cancellationToken);
        public void Deactivate(AssetType entity);
    }
}
