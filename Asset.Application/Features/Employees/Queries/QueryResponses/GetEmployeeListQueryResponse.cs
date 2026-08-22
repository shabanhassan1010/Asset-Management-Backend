namespace Asset.Application.Features.Employees.Queries.QueryResponses
{
    public class GetEmployeeListQueryResponse
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }
}
