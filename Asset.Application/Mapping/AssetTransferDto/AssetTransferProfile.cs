using Asset.Application.Features.AssetTransfers.Queries.QueryResponses;
using Asset.Domain.Models;
using AutoMapper;

namespace Asset.Application.Mapping.AssetTransferDto
{
    public class AssetTransferProfile : Profile
    {
        public AssetTransferProfile()
        {
            CreateMap<AssetTransfer, GetAssetTransferHistoryResponse>()
                .ForMember(d => d.FromEmployeeName, o => o.MapFrom(s => s.FromEmployee == null ? null : s.FromEmployee.FullName))
                .ForMember(d => d.ToEmployeeName, o => o.MapFrom(s => s.ToEmployee == null ? null : s.ToEmployee.FullName))
                .ForMember(d => d.FromDepartmentName, o => o.MapFrom(s => s.FromDepartment == null ? null : s.FromDepartment.DepartmentName))
                .ForMember(d => d.ToDepartmentName, o => o.MapFrom(s => s.ToDepartment == null ? null : s.ToDepartment.DepartmentName))
                .ForMember(d => d.FromLocationName, o => o.MapFrom(s => s.FromLocation == null ? null : s.FromLocation.LocationName))
                .ForMember(d => d.ToLocationName, o => o.MapFrom(s => s.ToLocation == null ? null : s.ToLocation.LocationName))
                .ForMember(d => d.TransferDate, o => o.MapFrom(s => DateTime.SpecifyKind(s.TransferDate, DateTimeKind.Utc)));

            CreateMap<AssetTransfer, GetTransferDetailsResponse>()
                    .ForMember(d => d.AssetName, o => o.MapFrom(s => s.Asset.AssetName))
                    .ForMember(d => d.AssetCode, o => o.MapFrom(s => s.Asset.AssetCode))
                    .ForMember(d => d.SerialNumber, o => o.MapFrom(s => s.Asset.SerialNumber))
                    .ForMember(d => d.FromEmployeeName, o => o.MapFrom(s => s.FromEmployee.FullName))
                    .ForMember(d => d.ToEmployeeName, o => o.MapFrom(s => s.ToEmployee.FullName))
                    .ForMember(d => d.FromDepartmentName, o => o.MapFrom(s => s.FromDepartment.DepartmentName))
                    .ForMember(d => d.ToDepartmentName, o => o.MapFrom(s => s.ToDepartment.DepartmentName))
                    .ForMember(d => d.FromLocationName, o => o.MapFrom(s => s.FromLocation.LocationName))
                    .ForMember(d => d.ToLocationName, o => o.MapFrom(s => s.ToLocation.LocationName))
                    .ForMember(d => d.TransferDate, o => o.MapFrom(s => DateTime.SpecifyKind(s.TransferDate, DateTimeKind.Utc)))
                    .ForMember(d => d.TransferredByName, o => o.Ignore());
        }
    }
}