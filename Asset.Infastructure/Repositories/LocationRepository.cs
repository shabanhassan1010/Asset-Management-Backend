#region
using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Enum;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
using AssetEntity = Asset.Domain.Models.Asset;
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
        public async Task<IReadOnlyList<GetLocationListResponse>> GetAllProjectedAsync(CancellationToken ct)
        {
            var query = _dbContext.Locations.AsNoTracking().Where(c => c.IsActive);

            return await query.OrderBy(c => c.LocationName)
                        .Select(c => new GetLocationListResponse
                        {
                            Id = c.Id,
                            LocationName = c.LocationName,
                            Address = c.Address,
                            IsActive = c.IsActive,
                            AssetsCount = c.Assets.Count()
                        }).ToListAsync(ct);
        }
        public async Task<(List<GetLocationListResponse> Items, int TotalCount)> GetPagedAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _dbContext.Locations.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => e.LocationName.Contains(search));
            }

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(t => t.LocationName)
                .ThenBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new GetLocationListResponse
                {
                    Id = t.Id,
                    LocationName = t.LocationName,
                    Address = t.Address,
                    IsActive = t.IsActive,
                    AssetsCount = t.Assets.Count(a => a.Status != (int)AssetStatus.Retired),
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }
        public async Task<IReadOnlyList<AssetEntity>> GetTrackedAssetsByLocationAsync(int locationId, CancellationToken ct)
        {
            return await _dbContext.Assets
                        .Where(a => a.LocationId == locationId)
                        .ToListAsync(ct);
        }

        // Check
        public async Task<bool> LocationNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Locations.AsNoTracking().Where(l => l.IsActive && l.LocationName == name);
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
        public void DeActivate(Location entity)
        {
            entity.IsActive = false;
        }
        #endregion
    }
}
