using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Domain.Models;
using AutoMapper;
namespace Asset.Application.Mapping.AssetTypeDto
{
    public class AssetTypeProfile : Profile
    {
        public AssetTypeProfile()
        {
            CreateMap<AssetType, GetAssetTypeListQueryResponse>()
                .ForMember(dest => dest.AssetTypeName, opt => opt.MapFrom(src=>src.TypeName));
        }
    }
}