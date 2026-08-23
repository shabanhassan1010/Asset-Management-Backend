using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Category.Queries.QueryResponse;
using MediatR;

namespace Asset.Application.Features.Category.Queries.QueryModels
{
    public class GetCategoryListQueryModel : IRequest<ApiResponse<IReadOnlyList<GetCategoryListResponse>>> , ICachedQuery
    {
        public string CacheKey => CacheKeys.CategoryList;
        public TimeSpan Duration => TimeSpan.FromMinutes(30);
    }
}
