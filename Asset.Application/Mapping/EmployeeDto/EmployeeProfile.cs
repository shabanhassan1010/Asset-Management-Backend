using Asset.Application.Features.Employees.Commands.CommandResponse;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using Asset.Domain.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Application.Mapping.EmployeeDto
{
    public partial class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            GetEmployeeByIdQueryMapping();
            GetEmployeeListQueryMapping();
            CreateEmployeeCommandMapping();
            UpdateEmployeeCommandMapping();
            SetEmployeeStatusCommandMapping();
            GetEmployeePaginatedListQueryMapping();
        }

        private void GetEmployeeByIdQueryMapping()
        {
            CreateMap<Employee, GetEmployeeByIdResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.DepartmentName));
        }
        private void GetEmployeeListQueryMapping()
        {
            CreateMap<Employee, GetEmployeeListQueryResponse>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department == null ? null : src.Department.DepartmentName))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName == null ? null : src.FullName));
        }
        private void CreateEmployeeCommandMapping()
        {
            CreateMap<Employee, CreateEmployeeCommandResponse>();
        }
        private void UpdateEmployeeCommandMapping()
        {
            CreateMap<Employee, UpdateEmployeeCommandResponse>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.DepartmentName));
        }
        private void SetEmployeeStatusCommandMapping()
        {
            CreateMap<Employee, SetEmployeeStatusCommandResponse>();
        }
        private void GetEmployeePaginatedListQueryMapping()
        {
            CreateMap<Employee, GetEmployeeListQueryResponse>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.DepartmentName));
        }
    }
}
