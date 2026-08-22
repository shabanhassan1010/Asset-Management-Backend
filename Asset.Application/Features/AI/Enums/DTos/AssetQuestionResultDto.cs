using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Asset.Application.Features.AI.Enums.DTos
{
    public class AssetQuestionResultDto
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? AssetType { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? LocationName { get; set; }

        // R2.6 / R4.3.
        //
        // For a non-admin this stays null, and WhenWritingNull means the property
        // is not serialised at all - the JSON that reaches the browser has no
        // purchaseCost key whatsoever. Not zero, not null: absent.
        // A user inspecting the network tab learns nothing about the field's existence.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? PurchaseCost { get; set; }
    }
}
