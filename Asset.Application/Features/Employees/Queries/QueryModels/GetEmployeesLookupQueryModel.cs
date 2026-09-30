using Asset.Application.Common.Caching;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeesLookupQueryModel : IRequest<IReadOnlyList<AvailableEmployeeDto>>
    {
        public string CacheKey => CacheKeys.EmployeeList;      // ← add: "employees:list"
        public TimeSpan Duration => CacheDurations.Lookup;     // ← add: same TTL as the other lookups
    }
}