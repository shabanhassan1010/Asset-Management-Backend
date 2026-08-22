namespace Asset.Application.Common.Caching
{
    public static class CacheKeys
    {
        public const string CategoryList = "categories:list";
        public static string CategoryById(int id) => $"categories:{id}";

        public const string LocationList = "locations:list";
        public static string LocationById(int id) => $"locations:{id}";

        public const string DepartmentList = "departments:list";
        public static string DepartmentById(int id) => $"departments:{id}";

        public const string EmployeeList = "employees:list";

        public const string AssetTypeList = "assettypes:list";

        /// <summary>
        /// The lookup lists carry AssetsCount, so creating, deleting or moving
        /// an asset makes all three stale. Asset commands clear them together.
        /// </summary>
        public static readonly string[] ListsAffectedByAssetChanges =
        {
            CategoryList,
            LocationList,
            DepartmentList
        };
    }
}
