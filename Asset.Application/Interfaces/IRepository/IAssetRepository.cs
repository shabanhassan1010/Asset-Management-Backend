using Asset.Application.Common.Responses;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Features.GetAssetTransferHistory.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;
using System.Linq.Expressions;
using AssetEntity = Asset.Domain.Models.Asset;
namespace Asset.Application.Interfaces.Repository
{
    public interface IAssetRepository : IBaseRepository<AssetEntity>   , 
                                        IActiveRepository<AssetEntity> , 
                                        ICheckRepository<AssetEntity>
    {
        // read
        Task<AssetEntity?> GetByIdWithDetailsAsync(int id, CancellationToken ct);
        Task<PagedResult<AssetEntity>> GetPaginationAsync(AssetFilter filter, CancellationToken ct);
        Task<AssetEntity?> GetForUpdateAsync(int id, CancellationToken ct);     // tracked, for writes
        Task<List<GetAssetTransferHistoryResponse>> GetTransferHistoryAsync(int assetId, CancellationToken ct);
        void SetOriginalRowVersion(AssetEntity entity, byte[] rowVersion);

        // Check
        Task<bool> SerialNumberExistsAsync(string serial, int? exceptId, CancellationToken ct);
        Task<bool> AnyAsync(Expression<Func<AssetEntity, bool>> predicate, CancellationToken cancellationToken);

        // Add
        public Task AddTransferAsync(AssetTransfer transfer, CancellationToken ct);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    }
}
