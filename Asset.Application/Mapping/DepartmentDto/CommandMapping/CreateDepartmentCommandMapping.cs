using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using Asset.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Application.Mapping.DepartmentDto
{
    public partial class DepartmentProfile
    {
        public void CreateDepartment()
        {
            CreateMap<CreateDepartmentCommandModel, Department>();
            CreateMap<Department, CreateDepartmentResponseDto>();
        }
    }
}
