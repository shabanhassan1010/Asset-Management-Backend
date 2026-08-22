#region
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class AssetTypeRepository(AssetManagementDbContext context) : IAssetTypeRepository
    {
        public async Task<IReadOnlyList<AssetType>> GetAllAsync(CancellationToken cancellationToken) =>
            await context.AssetTypes
                .AsNoTracking()
                .OrderBy(t => t.TypeName)
                .ToListAsync(cancellationToken);
    }
}
