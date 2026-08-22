using Asset.Application.Common.Responses;
using MediatR;
namespace Asset.Application.Features.Departments.Commands.CommandModels
{
    public class DeleteDepartmentCommandModel : IRequest<ApiResponse<string>>
    {
        public int Id { get; set; }

        public DeleteDepartmentCommandModel(int id)
        {
            Id = id;
        }
    }
}
