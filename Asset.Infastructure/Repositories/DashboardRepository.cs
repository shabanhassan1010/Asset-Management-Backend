#region 
using Asset.Application.Features.Dashboard.DTos;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Enum;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constrcutor
        public DashboardRepository(AssetManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Private Methods
        private const int RetiredStatus = (int)AssetStatus.Retired;
        private IQueryable<AssetEntity> ActiveAssets => Assets.Where(a => a.Status != RetiredStatus);
        private IQueryable<AssetEntity> Assets => _dbContext.Assets.AsNoTracking();  // get All Asset from Database With Not Tracking 
        #endregion

        #region Methods
        // Get
        public async Task<IReadOnlyList<CategoryCountDto>> GetCountsByCategoryAsync(CancellationToken cancellationToken)
        {
          var rows = await ActiveAssets.GroupBy(a => a.Category!.CategoryName)
                                    .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                                    .OrderByDescending(x => x.Count)
                                    .ToListAsync(cancellationToken);

            return rows.Select(r => new CategoryCountDto(r.CategoryName, r.Count)).ToList();
        }
        public async Task<IReadOnlyList<ExpiringWarrantyDto>> GetExpiringWarrantiesAsync(DateOnly from, DateOnly to, int take, CancellationToken cancellationToken)
        {
            var rows = await ActiveAssets.Where(a => a.WarrantyExpiryDate != null && a.WarrantyExpiryDate >= from && a.WarrantyExpiryDate <= to)
                                         .OrderBy(a => a.WarrantyExpiryDate)
                                         .Take(take)                      
                                         .Select(a => new   // execute query now
                                         {
                                                a.Id,
                                                a.AssetCode,
                                                a.AssetName,
                                                a.Status,
                                                Expiry = a.WarrantyExpiryDate!.Value
                                         })
                                         .ToListAsync(cancellationToken);
            return rows
                .Select(r => new ExpiringWarrantyDto(
                    r.Id, 
                    r.AssetCode, 
                    r.AssetName, 
                    r.Status, 
                    ((AssetStatus)r.Status).ToString(),
                    r.Expiry))
                .ToList();
        }
        public async Task<AssetStatusCounts> GetStatusCountsAsync(CancellationToken cancellationToken)
        {
            var counts = await Assets.GroupBy(a => 1)
                                     .Select(g => new
                                     {
                                         Active = g.Count(a => a.Status != RetiredStatus),
                                         Retired = g.Count(a => a.Status == RetiredStatus),
                                         Available = g.Count(a => a.Status == (int)AssetStatus.Available),
                                         Assigned = g.Count(a => a.Status == (int)AssetStatus.Assigned),
                                         UnderMaintenance = g.Count(a => a.Status == (int)AssetStatus.UnderMaintenance)
                                     })
                                    .FirstOrDefaultAsync(cancellationToken);

            // If He do not find any Status return 0 instead of throw Exception
            return counts is null
            ? new AssetStatusCounts(0, 0, 0, 0, 0)
            : new AssetStatusCounts(counts.Active, counts.Retired, counts.Available,
                                    counts.Assigned, counts.UnderMaintenance);

        }
        public Task<decimal> GetTotalPurchaseCostAsync(CancellationToken cancellationToken)
        {
           return ActiveAssets.SumAsync(a => a.PurchaseCost ?? 0m, cancellationToken);
        }
        #endregion
    }
}