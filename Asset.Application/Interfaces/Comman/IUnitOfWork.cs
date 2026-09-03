#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
#endregion

namespace Asset.Application.Interfaces.Comman
{
    public interface IUnitOfWork
    {
        IAssetRepository Assets { get; }
        IAssetTransferRepository AssetTransfers { get; }
        IAssetTypeRepository AssetTypes { get; }
        ICategoryRepository Categories { get; }
        IDepartmentRepository Departments { get; }
        IEmployeeRepository Employees { get; }
        ILocationRepository Locations { get; }
        IDashboardRepository Dashboard { get; }
        IAiLookupRepository AiLookup { get; }
        Task<int> SaveChangesAsync(CancellationToken ct);
    }
}
