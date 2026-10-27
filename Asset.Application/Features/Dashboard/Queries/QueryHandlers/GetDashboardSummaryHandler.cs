#region
using Asset.Application.Features.Dashboard.DTos;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using MediatR;
#endregion

namespace Asset.Application.Features.Dashboard.Queries.QueryHandlers
{
    public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
    public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        #region Fields
        private const int WarrantyWindowInDays = 90;
        private const int MaxExpiringRows = 6;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService currentUser;
        #endregion

        #region Constructor
        public GetDashboardSummaryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            this.currentUser = currentUser;
        }
        #endregion

        #region Handlers
        public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var counts = await _unitOfWork.Dashboard.GetStatusCountsAsync(cancellationToken);
            var byCategory = await _unitOfWork.Dashboard.GetCountsByCategoryAsync(cancellationToken);
            var byDepartment = await _unitOfWork.Dashboard.GetCountsByDepartmentAsync(cancellationToken);
            var warrantyWindows = await _unitOfWork.Dashboard.GetWarrantyWindowCountsAsync(today, cancellationToken);
            var expiring = await _unitOfWork.Dashboard.GetExpiringWarrantiesAsync(today, today.AddDays(WarrantyWindowInDays), MaxExpiringRows, cancellationToken);


            // If User Not Admin PurchaseCost will not read
            decimal? portfolioValue = currentUser.IsAdmin ? await _unitOfWork.Dashboard.GetTotalPurchaseCostAsync(cancellationToken): null;

            return new DashboardSummaryDto
            {
                ActiveAssets = counts.Active,
                RetiredAssets = counts.Retired,
                AvailableAssets = counts.Available,
                AssignedAssets = counts.Assigned,
                UnderMaintenanceAssets = counts.UnderMaintenance,
                PortfolioValue = portfolioValue,
                AssetsByCategory = byCategory,
                AssetsByDepartment = byDepartment,
                WarrantyWindows = warrantyWindows,
                ExpiringWarranties = expiring
            };
        }
        #endregion
    }
}