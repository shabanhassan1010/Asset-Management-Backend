using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.CategoryDto
{
    public partial class CategortProfile
    {
        public void GetCategoryList()
        {
            CreateMap<Category, GetCategoryListResponse>()
                .ForMember(dest => dest.AssetsCount, opt => opt.MapFrom(src => src.Assets.Count));
        }
    }
}
