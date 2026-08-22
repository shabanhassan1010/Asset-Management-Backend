using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.DepartmentDto
{
    public partial class DepartmentProfile
    {
        public void UpdateDepartment()
        {
            CreateMap<UpdateDepartmentCommandModel, Department>()
                .ForMember(d => d.Id, opt => opt.Ignore());

            CreateMap<Department, UpdateDepartmentResponseDto>();
        }
    }
}
