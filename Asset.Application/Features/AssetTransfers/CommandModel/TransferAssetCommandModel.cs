using Asset.Application.Common.Responses;
using Asset.Application.Features.AssetTransfers.CommandResponse;
using MediatR;
using System.Text.Json.Serialization;
namespace Asset.Application.Features.AssetTransfers.CommandModel
{
    public class TransferAssetCommandModel : IRequest<ApiResponse<TransferAssetResponseDto>>
    {
        /// <summary>
        /// Comes from the route, not the body. JsonIgnore so a caller cannot
        /// target one asset in the URL and a different one in the payload -
        /// the same reasoning as UpdateAssetCommandModel.AssetId.
        /// </summary>
        [JsonIgnore]
        public int AssetId { get; set; }

        /// <summary>Null means "unassign". Sending 0 is treated as null too.</summary>
        public int? ToEmployeeId { get; set; }

        public int? ToDepartmentId { get; set; }

        public int? ToLocationId { get; set; }

        public DateTime TransferDate { get; set; }

        /// <summary>R3.1 - required, so the history explains WHY the asset moved.</summary>
        public string Reason { get; set; }

        /// <summary>
        /// R3.5 - the RowVersion the client last read, Base64 encoded. Two admins
        /// transferring the same asset at the same time both send the stamp they
        /// read; the second save finds it stale and fails with 409 instead of
        /// silently overwriting the first transfer.
        /// </summary>
        public string RowVersion { get; set; }
    }
}
