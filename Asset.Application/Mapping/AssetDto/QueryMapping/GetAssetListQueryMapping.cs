using Asset.Application.Features.Assets.Queries.QueryResponses;
using AssetEntity = Asset.Domain.Models.Asset;
namespace Asset.Application.Mapping.AssetDto
{
    public partial class AssetProfile
    {
        public void GetAssetListQueryMapping()
        {
            CreateMap<AssetEntity, GetAssetListQueryResponse>()
                .ForMember(dest => dest.AssetTypeName, opt => opt.MapFrom(src => src.AssetType.TypeName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.DepartmentName))
                // Arabic names: the cached response carries BOTH languages, the frontend shows the right one.
                .ForMember(dest => dest.CategoryNameAr, opt => opt.MapFrom(src => src.Category.CategoryNameAr))
                .ForMember(dest => dest.AssetTypeNameAr, opt => opt.MapFrom(src => src.AssetType.TypeNameAr))
                .ForMember(dest => dest.DepartmentNameAr, opt => opt.MapFrom(src => src.Department == null ? null : src.Department.DepartmentNameAr))
                .ForMember(dest => dest.EmployeeNameAr, opt => opt.MapFrom(src => src.AssignedEmployee == null ? null : src.AssignedEmployee.FullNameAr))
                .ForMember(dest => dest.LocationNameAr, opt => opt.MapFrom(src => src.Location == null ? null : src.Location.LocationNameAr));

        }
    }
}
