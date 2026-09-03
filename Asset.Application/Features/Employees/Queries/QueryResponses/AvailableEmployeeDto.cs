namespace Asset.Application.Features.Employees.Queries.QueryResponses
{
    public class AvailableEmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }  
        public int DepartmentId { get; set; }      
        public bool IsActive { get; set; }
    }
}
