using AutoMapper;
namespace Asset.Application.Mapping.CategoryDto
{
    public partial class CategortProfile : Profile
    {
        public CategortProfile()
        {
            GetCategory();
            GetCategoryList();
            UpdateCategory();
            CreateCategory();
        }

    }
}
