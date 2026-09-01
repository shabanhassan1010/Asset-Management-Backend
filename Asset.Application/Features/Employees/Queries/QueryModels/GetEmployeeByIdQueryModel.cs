using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeeByIdQueryModel : IRequest<ApiResponse<GetEmployeeByIdResponse>>  
    {
        public int Id { get; set; }
        public GetEmployeeByIdQueryModel(int id)
        {
            Id = id;
        }
    }
}