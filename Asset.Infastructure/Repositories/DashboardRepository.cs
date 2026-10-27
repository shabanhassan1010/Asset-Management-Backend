#region 
using Asset.Application.Features.Dashboard.DTos;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Enum;
using Asset.Infastructure.Extensions;
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
          var rows = await ActiveAssets.GroupBy(a => new {a.CategoryId,a.Category!.CategoryName})
                                       .Select(g => new { g.Key.CategoryId, g.Key.CategoryName, Count = g.Count() })
                                       .OrderByDescending(x => x.Count)
                                       .ToListAsync(cancellationToken);

            return rows.Select(r => new CategoryCountDto(r.CategoryId,r.CategoryName, r.Count)).ToList();
        }
        public async Task<IReadOnlyList<DepartmentCountDto>> GetCountsByDepartmentAsync(CancellationToken cancellationToken)
        {
            var rows = await ActiveAssets.GroupBy(a => new { a.DepartmentId, a.Department!.DepartmentName })
                                         .Select(g => new { g.Key.DepartmentId, g.Key.DepartmentName, Count = g.Count() })
                                         .OrderByDescending(x => x.Count)
                                         .ToListAsync(cancellationToken);

            // DepartmentId is nullable → those assets come back with a null name → give them a readable label
            return rows.Select(r => new DepartmentCountDto(r.DepartmentId, r.DepartmentName ?? "No department", r.Count)).ToList();
        }

        public async Task<IReadOnlyList<ExpiringWarrantyDto>> GetExpiringWarrantiesAsync(DateOnly from, DateOnly to, int take, CancellationToken cancellationToken)
        {
            var rows = await ActiveAssets.WarrantyExpiringBetween(from, to)
                                 .OrderBy(a => a.WarrantyExpiryDate)
                                 .Take(take)
                                 .Select(a => new
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
        public async Task<IReadOnlyList<WarrantyWindowDto>> GetWarrantyWindowCountsAsync(DateOnly today, CancellationToken cancellationToken)
        {
            var in30Days = today.AddDays(30);
            var in60Days = today.AddDays(60);
            var in90Days = today.AddDays(90);

            // One SQL query: keep only rows inside the widest window, then count each window with COUNT(CASE WHEN ...)
            var counts = await ActiveAssets.WarrantyExpiringBetween(today, in90Days)
                                           .GroupBy(a => 1)
                                           .Select(g => new
                                           {
                                               Within30 = g.Count(a => a.WarrantyExpiryDate <= in30Days),
                                               Within60 = g.Count(a => a.WarrantyExpiryDate <= in60Days),
                                               Within90 = g.Count()
                                           })
                                           .FirstOrDefaultAsync(cancellationToken);

            // No matching rows → null → return zeros instead of throwing
            return
            [
                new WarrantyWindowDto(30, counts?.Within30 ?? 0),
                new WarrantyWindowDto(60, counts?.Within60 ?? 0),
                new WarrantyWindowDto(90, counts?.Within90 ?? 0),
            ];
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