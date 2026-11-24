using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Commands.CommandResponse;
using MediatR;

namespace Asset.Application.Features.Employees.Commands.CommandModels
{
    public class CreateEmployeeCommandModel : IRequest<ApiResponse<CreateEmployeeCommandResponse>>
    {
        // No EmployeeCode: the server makes it (EmployeeCodes.Next), so it can't be duplicated or skipped.
        public string FullName { get; set; }
        public string? FullNameAr { get; set; }          // optional
        public string Email { get; set; }
        public string Phone { get; set; }
        public int DepartmentId { get; set; }
    }
}
