using Asset.Application.Common.Responses;
using Asset.Application.Features.Category.Commands.CommandResponse;
using MediatR;

namespace Asset.Application.Features.Category.Commands.CommandModels
{
    public class UpdateCategoryCommandModel : IRequest<ApiResponse<UpdateCategoryResponseDto>>
    {
        public int Id { get; set; }  
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
