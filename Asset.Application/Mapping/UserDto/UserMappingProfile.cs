using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Features.Users.DTOs;
using AutoMapper;

namespace Asset.Application.Mapping;

/// <summary>
/// Both maps start from UserWithRole rather than ApplicationUser, because the
/// role is not on the user entity. Mapping from the pair means no handler has
/// to patch the Role property in after the fact.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserWithRole, UserListItemDto>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.User.Id))
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.User.IsActive))
            .ForMember(d => d.EmployeeId, o => o.MapFrom(s => s.User.EmployeeId))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.User.CreatedAt))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role));

        CreateMap<UserWithRole, CurrentUserDto>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.User.Id))
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email))
            .ForMember(d => d.EmployeeId, o => o.MapFrom(s => s.User.EmployeeId))
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee != null ? s.Employee.FullName : null))
            .ForMember(d => d.EmployeeCode, o => o.MapFrom(s => s.Employee != null ? s.Employee.EmployeeCode : null))
            .ForMember(d=> d.DepartmentName , o=> o.MapFrom(s => s.Employee != null ? s.Employee.DepartmentName : null))
            .ForMember(d =>d.IsActive, o => o.MapFrom(s => s.User.IsActive))
            .ForMember(d => d.Phone, o => o.MapFrom(s => s.Employee != null ? s.Employee.Phone : null))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role));
    }
}
