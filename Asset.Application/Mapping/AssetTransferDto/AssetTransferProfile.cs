using Asset.Application.Features.GetAssetTransferHistory.QueryResponses;
using Asset.Domain.Models;
using AutoMapper;

namespace Asset.Application.Mapping.AssetTransferDto
{
    public class AssetTransferProfile : Profile
    {
        public AssetTransferProfile()
        {
            CreateMap<AssetTransfer, GetAssetTransferHistoryResponse>()
                .ForMember(d => d.FromEmployeeName,    o => o.MapFrom(s => s.FromEmployee == null ? null : s.FromEmployee.FullName))
                .ForMember(d => d.ToEmployeeName,      o => o.MapFrom(s => s.ToEmployee == null ? null : s.ToEmployee.FullName))
                .ForMember(d => d.FromDepartmentName,  o => o.MapFrom(s => s.FromDepartment == null ? null : s.FromDepartment.DepartmentName))
                .ForMember(d => d.ToDepartmentName,    o => o.MapFrom(s => s.ToDepartment == null ? null : s.ToDepartment.DepartmentName))
                .ForMember(d => d.FromLocationName,    o => o.MapFrom(s => s.FromLocation == null ? null : s.FromLocation.LocationName))
                .ForMember(d => d.ToLocationName,      o => o.MapFrom(s => s.ToLocation == null ? null : s.ToLocation.LocationName));
        }
    }
}