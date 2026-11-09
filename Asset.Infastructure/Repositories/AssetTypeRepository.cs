#region
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Enum;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class AssetTypeRepository : BaseRepository<AssetType>, IAssetTypeRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constructor
        public AssetTypeRepository(AssetManagementDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        public async Task<IReadOnlyList<AssetType>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.AssetTypes.AsNoTracking()
                                              .Where(s=>s.IsActive)
                                              .OrderBy(t => t.TypeName)
                                              .ToListAsync(cancellationToken);
        }
        public async Task<(List<GetAssetTypeListQueryResponse> Items, int TotalCount)> GetPagedAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _dbContext.AssetTypes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => e.TypeName.Contains(search));
            }

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(t => t.TypeName)
                .ThenBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new GetAssetTypeListQueryResponse
                {
                    Id = t.Id,
                    AssetTypeName = t.TypeName,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    AssetsCount = t.Assets.Count(
                        a => a.Status != (int)AssetStatus.Retired)
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }
        public async Task<bool> AnyAsync(Expression<Func<AssetType, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _dbContext.AssetTypes.AnyAsync(predicate, cancellationToken);
        }
        public async Task<bool> AssetTypeNameExistsAsync(string name, int? exceptId, CancellationToken cancellationToken)
        {
            var query = _dbContext.AssetTypes.Where(a=>a.IsActive && a.TypeName == name);

            if (exceptId.HasValue)
            {
                query = query.Where(t => t.Id != exceptId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
        public void Deactivate(AssetType entity)
        {
            entity.IsActive = false;
        }
    }
}