using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetAvailableEmployeesQueryModel : IRequest<IReadOnlyList<AvailableEmployeeDto>>
    {
        public readonly int? departmentId;

        public GetAvailableEmployeesQueryModel(int? DepartmentId)
        {
            departmentId = DepartmentId;
        }
    }
}
