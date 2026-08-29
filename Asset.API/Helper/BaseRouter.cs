namespace Asset.API.Helper
{
    public static partial class BaseRouter
    {
        public const string Root = "api";
        public const string Version = "v1";
        public const string Rule = Root + "/" + Version;
        public const string RouteId = "{id}";
        public static class AssetRouter
        {
            public const string Base = Rule + "/assets";
            public const string Id = Base + "/" + RouteId;
            public const string Retire = Id + "/retire";
            public const string Paginated = Base + "/paginated";
            public const string Transfers = Id + "/transfers";
        }
        public static class CategoryRouter
        {
            public const string Base = Rule + "/categories";
            public const string Id   = Base + "/" + RouteId;
        }
        public static class LocationRouter
        {
            public const string Base = Rule + "/locations";
            public const string Id = Base + "/" + RouteId;
        }
        public static class DepartmentRouter
        {
            public const string Base = Rule + "/departments";
            public const string Id = Base + "/" + RouteId;
        }
        public static class EmployeeRouter
        {
            public const string Paginated = Base + "/paginated";
            public const string Status = Id + "/status";
            public const string Id = Base + "/{id:int}";
            public const string Base = Rule + "/employees";
            public const string AvailableForUser = Base + "/available-for-user";
        }

        public static class AssetTypeRouter
        {
            public const string Base = Rule + "/asset-types";
            public const string Id = Base + "/{id:int}";
        }

        public static class UserRouter
        {
            public const string Base = Rule + "/users";
            public const string Id = Base + "/" + RouteId;
            public const string Role = Id + "/role";
            public const string Status = Id + "/status";
        }

        public static class AuthRouter
        {
            public const string Base =  Rule + "/auth";
            public const string Login = Base + "/login";
            public const string Refresh = Base + "/refresh";
            public const string Logout = Base + "/logout";
            public const string Me = Base + "/me";
        }

        public static class DashboardRouter
        {
            public const string Summary = Rule + "/dashboard/summary";
        }

        public static class AIRouter
        {
            public const string Base = Rule + "/ai";
            public const string Ask = Base + "/ask";
        }
    }
}