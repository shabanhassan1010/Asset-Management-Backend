using Asset.Application.Common.Responses;
using Asset.Application.Features.AssetTransfers.CommandResponse;
using MediatR;
using System.Text.Json.Serialization;
namespace Asset.Application.Features.AssetTransfers.CommandModel
{
    public class TransferAssetCommandModel : IRequest<ApiResponse<TransferAssetResponseDto>>
    {
        [JsonIgnore]
        public int AssetId { get; set; }
        public int? ToEmployeeId { get; set; }

        public int? ToDepartmentId { get; set; }

        public int? ToLocationId { get; set; }

        public DateTime TransferDate { get; set; }

        public string Reason { get; set; }
        public string RowVersion { get; set; }
    }
}
