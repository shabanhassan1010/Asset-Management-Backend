using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Features.Assets.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Assets.Queries.QueryModels
{
    public class GetAssetByIdQueryModel : IRequest<GetByIdQueryResponse>
    {
        public int Id { get; set; }
        public GetAssetByIdQueryModel(int id)
        {
            Id = id;
        }
    }
}
