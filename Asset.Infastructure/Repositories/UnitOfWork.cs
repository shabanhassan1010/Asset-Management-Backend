#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Infastructure.Models;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        #region Fields
        private readonly AssetManagementDbContext _context;
        private IAssetRepository? _assets;
        private IAssetTransferRepository? _assetTransfers;
        private IAssetTypeRepository? _assetTypes;
        private ICategoryRepository? _categories;
        private IDepartmentRepository? _departments;
        private IEmployeeRepository? _employees;
        private ILocationRepository? _locations;
        private IUserRepository? _users;
        private IRefreshTokenRepository? _refreshTokens;
        private IDashboardRepository? _dashboard;
        private IAiLookupRepository? _AilookupRepository;
        #endregion

        #region Constrcutor
        public UnitOfWork(AssetManagementDbContext context) => _context = context;
        #endregion

        #region Repos
        public IAssetRepository Assets => _assets ??= new AssetRepository(_context);
        public IAssetTransferRepository AssetTransfers => _assetTransfers ??= new AssetTransferRepository(_context);
        public IAssetTypeRepository AssetTypes => _assetTypes ??= new AssetTypeRepository(_context);
        public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
        public IDepartmentRepository Departments => _departments ??= new DepartmentRepository(_context);
        public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(_context);
        public ILocationRepository Locations => _locations ??= new LocationRepository(_context);
        public IDashboardRepository Dashboard=> _dashboard ??= new DashboardRepository(_context);
        public IAiLookupRepository AiLookup => _AilookupRepository ??= new AiLookupRepository(_context);
        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            // An explicit transaction is only needed when two saves must be joined.
            // UserManager saves on its own, so a role change plus a token revocation
            // is two saves - and both must land or neither.
            //
            // UserManager and the repositories share the same scoped
            // AppIdentityDbContext, so they enlist in this transaction automatically.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            await action();
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            // No rollback call: leaving the using block without committing rolls
            // back, so an exception thrown inside action() is already handled.
        }
        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            return _context.SaveChangesAsync(ct);
        }
        #endregion
    }
}
