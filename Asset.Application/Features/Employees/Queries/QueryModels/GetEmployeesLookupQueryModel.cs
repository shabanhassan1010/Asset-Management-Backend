using Asset.Application.Common.Caching;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeesLookupQueryModel : IRequest<IReadOnlyList<AvailableEmployeeDto>>, ICachedQuery
    {
        public string CacheKey => CacheKeys.EmployeeList;      // ← add: "employees:list"
        public CacheDuration Duration => CacheDuration.Lookup;     // ← add: same TTL as the other lookups
    }
}