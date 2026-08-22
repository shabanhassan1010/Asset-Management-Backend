namespace Asset.Application.Features.Departments.Queries.QueryResponse
{
    public class GetDepartmentListResponse
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string Code { get; set; }
        public int AssetsCount { get; set; }
        public int EmployeesCount { get; set; }
    }
}
