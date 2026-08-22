using Asset.Application.Common.Responses;
using Asset.Application.Features.Assets.Commands.CommandResponse;
using MediatR;
using AssetEntity = Asset.Domain.Models.Asset;

namespace Asset.Application.Features.Assets.Commands.CommandModels
{
    public class CreateAssetCommandModel : IRequest<ApiResponse<CreateAssetResponseDto>>
    {
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public int AssetTypeId { get; set; }
        public int Status { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public DateOnly? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public DateOnly? WarrantyExpiryDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? AssignedEmployeeId { get; set; }
        public int? LocationId { get; set; }
    }
}
