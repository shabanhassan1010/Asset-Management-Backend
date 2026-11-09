using Asset.Application.Common.Responses;
using MediatR;
namespace Asset.Application.Features.Departments.Commands.CommandModels
{
    public class DeactivateDepartmentCommandModel : IRequest<ApiResponse<string>>
    {
        public int Id { get; set; }

        public DeactivateDepartmentCommandModel(int id)
        {
            Id = id;
        }
    }
}
