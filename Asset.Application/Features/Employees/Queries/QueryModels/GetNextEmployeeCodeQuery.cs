using Asset.Application.Common.Responses;
using MediatR;

namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    // The code the next new employee will get, shown in the (disabled) "Employee code" field.
    // A record with no properties: the question has no parameters.
    // Not cached: it must count every employee saved up to this second.
    public record GetNextEmployeeCodeQuery : IRequest<ApiResponse<string>>;
}
