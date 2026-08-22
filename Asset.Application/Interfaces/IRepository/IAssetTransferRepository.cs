using Asset.Domain.Models;

namespace Asset.Application.Interfaces.IRepository
{
    public interface IAssetTransferRepository
    {
        Task<IReadOnlyList<AssetTransfer>> GetByAssetIdAsync(int assetId, CancellationToken cancellationToken);

    }
}
