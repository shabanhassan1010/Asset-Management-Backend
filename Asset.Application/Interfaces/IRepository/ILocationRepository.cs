#region
using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion


namespace Asset.Application.Interfaces.IRepository
{
    public interface ILocationRepository : IBaseRepository<Location> , 
                                           IActiveRepository<Location>
    {
        // Get
        Task<IReadOnlyList<GetLocationListResponse>> GetAllProjectedAsync(CancellationToken ct);
        Task<IReadOnlyList<AssetEntity>> GetTrackedAssetsByLocationAsync(int locationId, CancellationToken ct);

        // Check
        Task<bool> LocationNameExistsAsync(string name, int? exceptId, CancellationToken ct);

        // Delete (soft Delete)
        void Remove(Location entity);
    }
}
