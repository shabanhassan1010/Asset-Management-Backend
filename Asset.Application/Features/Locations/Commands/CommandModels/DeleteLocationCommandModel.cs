using Asset.Application.Common.Responses;
using MediatR;
namespace Asset.Application.Features.Locations.Commands.CommandModels
{
    public class DeleteLocationCommandModel : IRequest<ApiResponse<string>>
    {
        public int Id { get; set; }
        public DeleteLocationCommandModel(int id)
        {
            Id = id;
        }
    }
}
