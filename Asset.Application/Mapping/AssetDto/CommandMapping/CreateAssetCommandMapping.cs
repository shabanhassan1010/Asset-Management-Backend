using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Features.Assets.Commands.CommandResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetEntity = Asset.Domain.Models.Asset;


namespace Asset.Application.Mapping.AssetDto
{
    public partial class AssetProfile
    {
        private void CreateAssetQueryMapping()
        {   
            CreateMap<CreateAssetCommandModel, AssetEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.AssetType, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedEmployee, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.AssetTransfers, opt => opt.Ignore());

            // ده الناقص
            CreateMap<AssetEntity, CreateAssetResponseDto>()
                .ForMember(dest => dest.AssetId, opt => opt.MapFrom(src => src.Id));
        }

    }
}
