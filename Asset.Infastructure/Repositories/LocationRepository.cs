#region
using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Models;
using AssetEntity = Asset.Domain.Models.Asset;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class LocationRepository : BaseRepository<Location>, ILocationRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constructor
        public LocationRepository(AssetManagementDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Methods
        // Get
        public Task<List<GetLocationListResponse>> GetAllProjectedAsync(CancellationToken ct)
        {
            var query = _dbContext.Locations.AsNoTracking().Where(c => c.IsActive);

            return query
                .OrderBy(c => c.LocationName)
                .Select(c => new GetLocationListResponse
                {
                    Id = c.Id,
                    LocationName = c.LocationName,
                    Address = c.Address,
                    IsActive = c.IsActive,
                    AssetsCount = c.Assets.Count()
                })
                .ToListAsync(ct);

        }      
        public async Task<List<AssetEntity>> GetTrackedAssetsByLocationAsync(int locationId, CancellationToken ct)
        {
            return await _dbContext.Assets
                        .Where(a => a.LocationId == locationId)
                        .ToListAsync(ct);
        }

        // Check
        public async Task<bool> LocationNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Locations.AsNoTracking().Where(l => l.LocationName == name);
            if (exceptId.HasValue)
            {
                query = query.Where(l => l.Id != exceptId.Value);
            }

            return await query.AnyAsync(ct);
        }
        public async Task<bool> ExistsActiveAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Locations.AnyAsync(a => a.Id == id && a.IsActive ,ct);
        }

        // Delete
        public void Remove(Location entity)
        {
            entity.IsActive = false;
        }
        #endregion
    }
}
