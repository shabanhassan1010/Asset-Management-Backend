using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Features.Category.Commands.CommandResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.CategoryDto
{
    public partial class CategortProfile
    {
        public void UpdateCategory()
        {
            CreateMap<UpdateCategoryCommandModel, Category>();
            CreateMap<Category, UpdateCategoryResponseDto>();
        }
    }
}
