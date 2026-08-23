using Asset.Application.Common.Caching;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;

namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeeListQueryModel : IRequest<IReadOnlyList<GetEmployeeListQueryResponse>>, ICachedQuery
    {
        public string CacheKey => CacheKeys.CategoryList;
        public TimeSpan Duration => TimeSpan.FromMinutes(30);
    }
}
