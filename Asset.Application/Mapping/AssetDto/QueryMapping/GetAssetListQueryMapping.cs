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
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.DepartmentName));

        }
    }
}
