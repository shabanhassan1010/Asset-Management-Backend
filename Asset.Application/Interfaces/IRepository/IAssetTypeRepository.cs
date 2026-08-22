using Asset.Domain.Models;
namespace Asset.Application.Interfaces.IRepository
{
    public interface IAssetTypeRepository
    {
        Task<IReadOnlyList<AssetType>> GetAllAsync(CancellationToken cancellationToken);
    }
}
