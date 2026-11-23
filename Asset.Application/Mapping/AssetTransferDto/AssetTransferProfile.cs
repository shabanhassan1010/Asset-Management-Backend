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
                // Arabic names (NULL when not filled in → the frontend falls back to English)
                .ForMember(d => d.FromEmployeeNameAr, o => o.MapFrom(s => s.FromEmployee == null ? null : s.FromEmployee.FullNameAr))
                .ForMember(d => d.ToEmployeeNameAr, o => o.MapFrom(s => s.ToEmployee == null ? null : s.ToEmployee.FullNameAr))
                .ForMember(d => d.FromDepartmentNameAr, o => o.MapFrom(s => s.FromDepartment == null ? null : s.FromDepartment.DepartmentNameAr))
                .ForMember(d => d.ToDepartmentNameAr, o => o.MapFrom(s => s.ToDepartment == null ? null : s.ToDepartment.DepartmentNameAr))
                .ForMember(d => d.FromLocationNameAr, o => o.MapFrom(s => s.FromLocation == null ? null : s.FromLocation.LocationNameAr))
                .ForMember(d => d.ToLocationNameAr, o => o.MapFrom(s => s.ToLocation == null ? null : s.ToLocation.LocationNameAr))
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
                    .ForMember(d => d.FromEmployeeNameAr, o => o.MapFrom(s => s.FromEmployee == null ? null : s.FromEmployee.FullNameAr))
                    .ForMember(d => d.ToEmployeeNameAr, o => o.MapFrom(s => s.ToEmployee == null ? null : s.ToEmployee.FullNameAr))
                    .ForMember(d => d.FromDepartmentNameAr, o => o.MapFrom(s => s.FromDepartment == null ? null : s.FromDepartment.DepartmentNameAr))
                    .ForMember(d => d.ToDepartmentNameAr, o => o.MapFrom(s => s.ToDepartment == null ? null : s.ToDepartment.DepartmentNameAr))
                    .ForMember(d => d.FromLocationNameAr, o => o.MapFrom(s => s.FromLocation == null ? null : s.FromLocation.LocationNameAr))
                    .ForMember(d => d.ToLocationNameAr, o => o.MapFrom(s => s.ToLocation == null ? null : s.ToLocation.LocationNameAr))
                    .ForMember(d => d.TransferDate, o => o.MapFrom(s => DateTime.SpecifyKind(s.TransferDate, DateTimeKind.Utc)))
                    .ForMember(d => d.TransferredByName,o => o.MapFrom(s => s.TransferredByUser == null ? null : s.TransferredByUser.UserName));
        }
    }
}