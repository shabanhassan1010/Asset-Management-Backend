using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.DepartmentDto
{
    public partial class DepartmentProfile
    {
        public void GetDepartment()
        {
            CreateMap<Department, GetDepartmentByIdResponse>();
        }
    }
}
