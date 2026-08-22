#region
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class AssetTransferRepository : IAssetTransferRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _context;
        #endregion

        #region Constructor
        public AssetTransferRepository(AssetManagementDbContext context)
        {
            _context = context;
        }
        #endregion

        #region Methods
        public async Task<IReadOnlyList<AssetTransfer>> GetByAssetIdAsync(int assetId, CancellationToken cancellationToken)
        {
            return await _context.AssetTransfers.AsNoTracking()
                .Where(t => t.AssetId == assetId)
                .Include(t => t.FromEmployee) // i use left join here to get data only not null value from any table 
                .Include(t => t.ToEmployee)
                .Include(t => t.FromDepartment)
                .Include(t => t.ToDepartment)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .OrderBy(t => t.TransferDate)
                .ThenBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }
        #endregion
    }
}
