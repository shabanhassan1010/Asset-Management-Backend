using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Commands.CommandResponse;
using MediatR;

namespace Asset.Application.Features.Employees.Commands.CommandModels
{
    public class SetEmployeeStatusCommandModel : IRequest<ApiResponse<SetEmployeeStatusCommandResponse>>
    {
        public int Id { get; set; }       
        public bool IsActive { get; set; }   
    }
}
