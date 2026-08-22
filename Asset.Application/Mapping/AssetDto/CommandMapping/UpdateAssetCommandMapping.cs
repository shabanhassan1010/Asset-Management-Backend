using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Features.Assets.Commands.CommandResponse;
using AssetEntity = Asset.Domain.Models.Asset;
namespace Asset.Application.Mapping.AssetDto
{
    public partial class AssetProfile
    {
        private void UpdateAssetCommandMapping()
        {
            CreateMap<UpdateAssetCommandModel, AssetEntity>()
                // الحقول دي مش بتتغير من الـ request خالص
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AssetCode, opt => opt.Ignore())   // ثابت بعد الإنشاء
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())  // SQL Server بيولّدها
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())     // بنحطها في الـ handler
                .ForMember(dest => dest.UpdatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.AssetType, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedEmployee, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.AssetTransfers, opt => opt.Ignore());

            CreateMap<AssetEntity, UpdateAssetResponseDto>()
                .ForMember(dest => dest.AssetId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RowVersion,
                           opt => opt.MapFrom(src => Convert.ToBase64String(src.RowVersion)));
        }
    }
}
