using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using MediatR;
namespace Asset.Application.Features.Departments.Commands.CommandModels
{
    public class UpdateDepartmentCommandModel : IRequest<ApiResponse<UpdateDepartmentResponseDto>>
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string Code { get; set; }
    }
}
