using Asset.Application.Common.Responses;
using MediatR;
namespace Asset.Application.Features.Category.Commands.CommandModels
{
    public class DeleteCategoryCommandModel : IRequest<ApiResponse<string>>
    {
        public DeleteCategoryCommandModel(int id)
        {
            Id = id;
        }
        public int Id { get; set; }
    }
}
