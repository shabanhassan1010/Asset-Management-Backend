using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.CategoryDto
{
    public partial class CategortProfile
    {
        public void GetCategory()
        {
            CreateMap<Category, GetCategoryByIdResponse>();
        }
    }
}
