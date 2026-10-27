using AssetEntity = Asset.Domain.Models.Asset;
namespace Asset.Infastructure.Extensions
{
    public static class AssetQueryExtensions
    {
        // Assets whose warranty ends between 'from' and 'to' (both included).
        // Assets with no warranty date are excluded: unknown is not expired.
        // Used by the dashboard counts, the dashboard table, and the asset list filter.
        public static IQueryable<AssetEntity> WarrantyExpiringBetween(this IQueryable<AssetEntity> query, DateOnly from, DateOnly to)
        {
            return query.Where(a => a.WarrantyExpiryDate != null && a.WarrantyExpiryDate >= from && a.WarrantyExpiryDate <= to);
        }
    }
}