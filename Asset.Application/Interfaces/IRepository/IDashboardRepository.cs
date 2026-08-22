using Asset.Application.Features.Dashboard.DTos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Application.Interfaces.IRepository
{
    public interface IDashboardRepository
    {
        Task<AssetStatusCounts> GetStatusCountsAsync(CancellationToken cancellationToken);
        // Admin Call 
        Task<decimal> GetTotalPurchaseCostAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<CategoryCountDto>> GetCountsByCategoryAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<ExpiringWarrantyDto>> GetExpiringWarrantiesAsync( DateOnly from, DateOnly to, int take, CancellationToken cancellationToken);
    }
}
