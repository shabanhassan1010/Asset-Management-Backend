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
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? PurchaseCost { get; set; }
    }
}