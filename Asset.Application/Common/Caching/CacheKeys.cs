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
        public static string EmployeeById(int id) => $"employees:{id}";

        public const string AssetTypeList = "assettypes:list";
        public static string AssetTypeById(int id) => $"assettypes:{id}";

        public const string AssetsVersion      = "assets:version";
        public const string EmployeesVersion   = "employees:version";
        public const string DepartmentsVersion = "departments:version";
        public const string AssetTypesVersion  = "assettypes:version";
        public const string CategoriesVersion  = "categories:version";
        public const string LocationsVersion   = "locations:version";


        public static string AssetById(int id) => $"assets:{id}";           

        public static readonly string[] ListsAffectedByAssetChanges =
        {
            CategoryList,
            LocationList,
            DepartmentList,
            AssetTypeList,
            EmployeeList
        };
        public static readonly string[] VersionsAffectedByAssetChanges =
        {
            AssetTypesVersion,
            CategoriesVersion,
            LocationsVersion,
            DepartmentsVersion
        };
    }
}