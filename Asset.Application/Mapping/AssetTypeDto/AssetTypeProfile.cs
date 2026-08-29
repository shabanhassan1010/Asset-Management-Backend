#region
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Domain.Models;
using AutoMapper;
#endregion
namespace Asset.Application.Mapping.AssetTypeDto
{
    public class AssetTypeProfile : Profile
    {
        public AssetTypeProfile()
        {
            CreateMap<AssetType, GetAssetTypeListQueryResponse>()
                      .ForMember(dest => dest.AssetTypeName, opt => opt.MapFrom(src => src.TypeName));

            // Read
            CreateMap<AssetType, GetAssetTypeListQueryResponse>()
                      .ForMember(dest => dest.AssetTypeName, opt => opt.MapFrom(src => src.TypeName));

            CreateMap<AssetType, GetAssetTypeByIdQueryResponse>()
                     .ForMember(dest => dest.AssetTypeName, opt => opt.MapFrom(src => src.TypeName));

            // Write
            CreateMap<CreateAssetTypeCommandModel, AssetType>();

            CreateMap<UpdateAssetTypeCommandModel, AssetType>()
                   .ForMember(dest => dest.Assets, opt => opt.Ignore());
        }
    }
}