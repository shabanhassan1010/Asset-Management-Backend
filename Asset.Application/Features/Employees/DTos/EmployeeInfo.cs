namespace Asset.Application.Features.Employees.DTos
{
    public record EmployeeInfo
    {
        public int Id { get; init; }
        public string EmployeeCode { get; init; }
        public string FullName { get; init; }
        public string Email { get; init; }
        public string Phone { get; init; }
        public int DepartmentId { get; init; }
        public string DepartmentName { get; init; } // drop if CurrentUserDto doesn't show it
        public bool IsActive { get; init; }
    }
}