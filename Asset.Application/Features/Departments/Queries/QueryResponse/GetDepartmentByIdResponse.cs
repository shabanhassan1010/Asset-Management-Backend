namespace Asset.Application.Features.Departments.Queries.QueryResponse
{
    public class GetDepartmentByIdResponse
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string? DepartmentNameAr { get; set; }
        public string Code { get; set; }
    }
}
