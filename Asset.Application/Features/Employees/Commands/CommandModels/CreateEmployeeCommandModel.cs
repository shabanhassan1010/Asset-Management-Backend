using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Commands.CommandResponse;
using MediatR;

namespace Asset.Application.Features.Employees.Commands.CommandModels
{
    public class CreateEmployeeCommandModel : IRequest<ApiResponse<CreateEmployeeCommandResponse>>
    {
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int DepartmentId { get; set; }
    }
}
