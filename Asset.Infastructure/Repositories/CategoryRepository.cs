#region
using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
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
        public Task<List<GetCategoryListResponse>> GetAllProjectedAsync(CancellationToken ct)
        {
            var query = _dbContext.Categories.AsNoTracking().Where(c => c.IsActive);

            return query
                .OrderBy(c => c.CategoryName)
                .Select(c => new GetCategoryListResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    AssetsCount = c.Assets.Count()
                })
                .ToListAsync(ct);
        }
        public async Task<bool> HasAssetsAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Assets.AsNoTracking().AnyAsync(a => a.CategoryId == id, ct);
        }

        // Check
        public async Task<bool> CategoryNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Categories.AsNoTracking().Where(c => c.CategoryName == name);
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
        public void Remove(Category entity)
        {
            entity.IsActive = false;
        }
        #endregion
    }
}
