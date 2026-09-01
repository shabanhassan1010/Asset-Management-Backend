#region
using Asset.Application.Interfaces.IRepository;
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
            return await _dbContext.AssetTypes.AsNoTracking().OrderBy(t => t.TypeName)
                                              .ToListAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<AssetType, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _dbContext.AssetTypes.AnyAsync(predicate, cancellationToken);
        }

        public void Remove(AssetType entity)
        {
            entity.IsActive = false;
        }
    }
}