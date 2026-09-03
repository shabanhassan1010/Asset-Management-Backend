using Asset.Domain.Enum;

namespace Asset.Application.Features.AI.Enums.DTos
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
        public bool HasAnyFilter => AssetTypeName  is not null || DepartmentName is not null || EmployeeName   is not null
                                                               || Manufacturer   is not null || Status         is not null;
    }
}