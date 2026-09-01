#region
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;
using System.Linq.Expressions;
#endregion
namespace Asset.Application.Interfaces.IRepository
{
    public interface IAssetTypeRepository : IBaseRepository<AssetType>
    {
        Task<IReadOnlyList<AssetType>> GetAllAsync(CancellationToken cancellationToken);
        Task<bool> AnyAsync(Expression<Func<AssetType, bool>> predicate, CancellationToken cancellationToken);
        public void Remove(AssetType entity);
    }
}
