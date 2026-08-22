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
        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            return _context.SaveChangesAsync();
        }
        #endregion
    }
}
