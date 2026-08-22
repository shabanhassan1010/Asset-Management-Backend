using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Features.Category.Commands.CommandResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.CategoryDto
{
    public partial class CategortProfile
    {
        public void CreateCategory()
        {
            // For Add The Data From Req into Entity 
            CreateMap<CreateCategoryCommandModel, Category>();

            // To return Dto 
            CreateMap<Category, CreateCategoryResponseDto>();
        }
    }
}
