using AutoMapper;
namespace Asset.Application.Mapping.DepartmentDto
{
    public partial class DepartmentProfile : Profile
    {
        public DepartmentProfile()
        {
            CreateDepartment();
            UpdateDepartment();
            GetDepartment();
            // List not need mapping Because i use projection in query
        }
    }
}
