#region
using Asset.Application.Common.Responses;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Features.GetAssetTransferHistory.QueryResponses;
using Asset.Application.Interfaces.Repository;
using Asset.Domain.Enum;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion
namespace Asset.Infastructure.Repositories
{
    public class AssetRepository : BaseRepository<AssetEntity>, IAssetRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constructor
        public AssetRepository(AssetManagementDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Private Methods
        // Note: This method is used to include related entities when querying assets.
        // It uses the Include method to specify the related entities to be included in the query results.
        private IQueryable<AssetEntity> WithJoins() => _dbContext.Assets
                            .Include(a => a.Category)
                            .Include(a => a.AssetType)
                            .Include(a => a.Department)
                            .Include(a => a.AssignedEmployee)
                            .Include(a => a.Location);
        private static IQueryable<AssetEntity> ApplySorting(IQueryable<AssetEntity> query, string? sortBy, bool sortDesc)  
        {
            return sortBy?.ToLowerInvariant()  // Convert sortBy to lowercase and choose the matching sorting case.
                   switch 
                   {
                     "assetname" => sortDesc // Check if the user wants to sort by AssetName.
                           ? query.OrderByDescending(a => a.AssetName)  // Sort AssetName from Z to A.
                            : query.OrderBy(a => a.AssetName),          // Sort AssetName from A to Z.

                     "purchasedate" => sortDesc 
                            ? query.OrderByDescending(a => a.PurchaseDate) 
                            : query.OrderBy(a => a.PurchaseDate),           

                     _ => sortDesc // If sortBy is null, empty, or an unsupported value, use AssetCode as the default.
                            ? query.OrderByDescending(a => a.AssetCode) 
                            : query.OrderBy(a => a.AssetCode)        
                   };
        }
        private static IQueryable<AssetEntity> ApplyFilters(IQueryable<AssetEntity> query, AssetFilter filter)     
        {
            if (!filter.IncludeRetired)
            {
                query = query.Where(a => a.Status != (int)AssetStatus.Retired);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search)) // Check if the user provided a non-empty search value.
            {
                var search = filter.Search.Trim(); // Remove spaces from the beginning and end of the search text.

                query = query.Where(a =>  a.AssetCode.Contains(search) ||    a.AssetName.Contains(search) ||   
                                   (a.SerialNumber != null &&  a.SerialNumber.Contains(search)));
            }          

            if (filter.CategoryId.HasValue) // Check if the user selected a Category.
            {
                query = query.Where(a => a.CategoryId == filter.CategoryId.Value); // Keep assets belonging to the selected Category.
            }

            if (filter.AssetTypeId.HasValue) // Check if the user selected an Asset Type.
            {
                query = query.Where(a => a.AssetTypeId == filter.AssetTypeId.Value); // Keep assets belonging to the selected Asset Type.
            }

            if (!string.IsNullOrWhiteSpace(filter.Manufacturer))
            {
                query = query.Where(a => a.Manufacturer == filter.Manufacturer);
            }

            if (filter.StatusId.HasValue) // Check if the user selected a Status.
            {
                query = query.Where(a => a.Status == filter.StatusId.Value); // Keep assets having the selected Status.
            }

            if (filter.DepartmentId.HasValue) // Check if the user selected a Department.
            {
                query = query.Where(a => a.DepartmentId == filter.DepartmentId.Value); // Keep assets belonging to the selected Department.
            }

            if (filter.LocationId.HasValue) // Check if the user selected a Location.
            {
                query = query.Where(a => a.LocationId == filter.LocationId.Value); // Keep assets belonging to the selected Location.
            }

            if (filter.EmployeeId.HasValue) // Check if the user selected an Employee.
            {
                query = query.Where(a =>  a.AssignedEmployeeId == filter.EmployeeId.Value); // Keep assets assigned to the selected Employee.
            }

            return query; // Return the query after applying all requested filters.
        }
        #endregion

        #region Methods
        // Get
        public Task<AssetEntity?> GetByIdWithDetailsAsync(int id, CancellationToken ct)
        {
            return WithJoins().AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        }
        public async Task<PagedResult<AssetEntity>> GetPaginationAsync(AssetFilter filter, CancellationToken ct)
        {
            var query = WithJoins().AsNoTracking();

            query = ApplyFilters(query, filter);

            var total = await query.CountAsync(ct);

            query = ApplySorting(query,filter.SortBy,filter.SortDesc);

            var items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);

            return new PagedResult<AssetEntity>
            {
                Items = items,             // Assets for the current page.
                TotalCount = total,        // Total matching assets before pagination.
                Page = filter.Page,        // Current page number.
                PageSize = filter.PageSize // Number of assets per page.
            };
        }
        public Task<List<GetAssetTransferHistoryResponse>> GetTransferHistoryAsync(int assetId, CancellationToken ct)
        {
            return _dbContext.AssetTransfers
                .AsNoTracking()
                .Where(t => t.AssetId == assetId)
                .OrderByDescending(t => t.TransferDate)     // الأحدث الأول
                .Select(t => new GetAssetTransferHistoryResponse
                {
                    Id = t.Id,
                    TransferDate = t.TransferDate,

                    // navigation properties — EF بيترجمها LEFT JOIN تلقائياً.
                    // بنستخدم null-conditional لأن المواقع/الأقسام optional
                    FromLocationName = t.FromLocation.LocationName,
                    ToLocationName = t.ToLocation.LocationName,
                    FromDepartmentName = t.FromDepartment.DepartmentName,
                    ToDepartmentName = t.ToDepartment.DepartmentName
                })
                .ToListAsync(ct);
        }
        public Task<AssetEntity?> GetForUpdateAsync(int id, CancellationToken ct)
        {
            return _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        // Add 
        public async Task AddTransferAsync(AssetTransfer transfer, CancellationToken ct)
        {
            await _dbContext.AssetTransfers.AddAsync(transfer, ct);
        }
        public void SetOriginalRowVersion(AssetEntity entity, byte[] rowVersion)
        {
            _dbContext.Entry(entity).Property(a => a.RowVersion).OriginalValue = rowVersion;
        }

        // Check
        public async Task<bool> SerialNumberExistsAsync(string serial, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Assets.AsNoTracking().Where(a => a.SerialNumber == serial);

            if (exceptId.HasValue)
            {
                query = query.Where(a => a.Id != exceptId.Value);
            }

            return await query.AnyAsync(ct);
        }
        public Task<bool> IsNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Assets.AsNoTracking().Where(a => a.AssetName == name);
            if (exceptId.HasValue)
            {
                query = query.Where(a => a.Id != exceptId.Value);
            }
            return query.AnyAsync(ct);
        }
        public Task<bool> IsCodeExistsAsync(string code, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Assets.AsNoTracking().Where(a => a.AssetCode == code);
            if (exceptId.HasValue)  // exceptId in create is null, in update it is the id of the asset being updated
            {
                query = query.Where(a => a.Id != exceptId.Value);
            }
            return query.AnyAsync(ct);
        }
        public async Task<bool> ExistsActiveAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Assets.AnyAsync(a => a.Id == id , ct);
        }
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
        {
            return _dbContext.Assets.AsNoTracking().AnyAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<AssetEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _dbContext.Assets.AnyAsync(predicate, cancellationToken);
        }
        #endregion
    }
}
