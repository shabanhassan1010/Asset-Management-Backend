namespace Asset.Application.Features.Employees.Commands.CommandResponse
{
    public class SetEmployeeStatusCommandResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
    }
}
