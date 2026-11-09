#region
using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Domain.Enum;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constructor
        public CategoryRepository(AssetManagementDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Methods

        // Get
        public async Task<IReadOnlyList<GetCategoryListResponse>> GetAllProjectedAsync(CancellationToken ct)
        {
            var query = _dbContext.Categories.AsNoTracking().Where(c => c.IsActive);

            return await query
                .OrderBy(c => c.CategoryName)
                .Select(c => new GetCategoryListResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    AssetsCount = c.Assets.Count(a => a.Status != (int)AssetStatus.Retired)
                })
                .ToListAsync(ct);
        }
        public async Task<(List<GetCategoryListResponse> Items, int TotalCount)> GetPagedAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _dbContext.Categories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => e.CategoryName.Contains(search));
            }

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(t => t.CategoryName)
                .ThenBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new GetCategoryListResponse
                {
                    Id = t.Id,
                    CategoryName = t.CategoryName,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    AssetsCount = t.Assets.Count(a => a.Status != (int)AssetStatus.Retired),
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }
        public async Task<bool> HasAssetsAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Assets.AsNoTracking().AnyAsync(a => a.CategoryId == id && a.Status != (int)AssetStatus.Retired, ct);
        }

        // Check
        public async Task<bool> CategoryNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Categories.AsNoTracking().Where(c => c.CategoryName == name && c.IsActive);
            if (exceptId.HasValue)
            {
                query = query.Where(c => c.Id != exceptId.Value);
            }

            return await query.AnyAsync(ct);
        }
        public async Task<bool> ExistsActiveAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Assets.AnyAsync(a => a.Id == id, ct);
        }

        // Delete
        public void DeActivate(Category entity)
        {
            entity.IsActive = false;
        }
        #endregion
    }
}