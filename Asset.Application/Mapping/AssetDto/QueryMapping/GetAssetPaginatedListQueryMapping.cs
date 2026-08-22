using Asset.Application.Features.Assets.Queries.QueryResponses;
using Asset.Domain.Enum;
using AssetEntity = Asset.Domain.Models.Asset;
namespace Asset.Application.Mapping.AssetDto
{
    public partial class AssetProfile
    {
        public void GetAssetPaginatedListQueryMapping()
        {
            CreateMap<AssetEntity, GetAssetPaginatedListQueryResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => Convert.ToBase64String(src.RowVersion)))
                .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location == null ? null : src.Location.LocationName))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.AssignedEmployee == null ? null : src.AssignedEmployee.FullName))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
                .ForMember(dest => dest.AssetTypeName, opt => opt.MapFrom(src => src.AssetType.TypeName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department == null ? null : src.Department.DepartmentName))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.AssignedEmployeeId))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => ((AssetStatus)src.Status).ToString()))
                .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status));

        }
    }
}
