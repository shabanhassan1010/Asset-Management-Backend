using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeesLookupQueryModel : IRequest<IReadOnlyList<AvailableEmployeeDto>>
    {

    }
}