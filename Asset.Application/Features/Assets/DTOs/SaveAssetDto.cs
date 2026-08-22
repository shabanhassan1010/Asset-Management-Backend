using System.ComponentModel.DataAnnotations;
namespace Asset.Application.Features.Assets.DTOs
{
    // What the client sends on Create and Update.
    // Notice what is NOT here: IsRetired, CreatedBy, or any role field.
    // The client cannot set those.
    public class SaveAssetDto
    {
        [Required, MaxLength(30)]
        public string AssetCode { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string AssetName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int CategoryId { get; set; }
        public int AssetTypeId { get; set; }
        public byte StatusId { get; set; }

        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }

        public DateOnly? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public DateOnly? WarrantyExpiryDate { get; set; }

        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
        public int? LocationId { get; set; }

        public string? RowVersion { get; set; }   // used by Update only
    }
}
