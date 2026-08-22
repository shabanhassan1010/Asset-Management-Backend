using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using MediatR;

namespace Asset.Application.Features.Departments.Commands.CommandModels
{
    public class CreateDepartmentCommandModel : IRequest<ApiResponse<CreateDepartmentResponseDto>>
    {
        public string DepartmentName { get; set; }
        public string Code { get; set; }
    }

}
