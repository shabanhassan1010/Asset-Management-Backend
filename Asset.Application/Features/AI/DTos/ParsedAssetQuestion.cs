using Asset.Application.Features.AI.Enums;
using Asset.Domain.Enum;

namespace Asset.Application.Features.AI.DTos
{
    public record ParsedAssetQuestion
    {
        public AssetQuestionIntent Intent { get; init; }
        public string? AssetTypeName { get; init; }
        public string? DepartmentName { get; init; }
        public string? EmployeeName { get; init; }
        public string? Manufacturer { get; init; }
        public AssetStatus? Status { get; init; }
        public bool IsAboutSelf { get; init; }
        // He will return true if any of the filter properties (AssetTypeName, DepartmentName, EmployeeName, Manufacturer, Status) are not null. Otherwise, it will return false.
        public bool HasAnyFilter => AssetTypeName  is not null ||
                                    DepartmentName is not null ||
                                    EmployeeName   is not null ||
                                    Manufacturer   is not null ||
                                    Status         is not null || 
                                    IsAboutSelf;
    }
}