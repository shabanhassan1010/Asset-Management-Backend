using Asset.Domain.Enum;

namespace Asset.Application.Features.AI.Enums.DTos
{
    public record ParsedAssetQuestion
    {
        public AssetQuestionIntent Intent { get; init; }

        // Names exactly as the user wrote them. They are still just text here -
        // the handler resolves them into real Ids against the database.
        // If a name does not exist, we answer politely instead of throwing (R4.5).
        public string? AssetTypeName { get; init; }
        public string? DepartmentName { get; init; }
        public string? EmployeeName { get; init; }

        // Manufacturer is a plain column on Assets (nvarchar(100)), not a foreign key,
        // so it goes straight into the filter with no lookup.
        public string? Manufacturer { get; init; }

        // Status is an int column on Assets backed by the existing AssetStatus enum,
        // so the parser can produce the final value directly - no lookup needed.
        public AssetStatus? Status { get; init; }

        // writing EmployeeName = "me". The parser has no idea who the caller is and must not pretend to:
        // it reports only that the question is self-directed, and the handler - which reads the token - decides whose id that means.
        public bool IsAboutSelf { get; init; }
        // Convenience for the handler: a question with no filters at all
        // ("show me stuff") is too vague to answer usefully.
        public bool HasAnyFilter => AssetTypeName  is not null || DepartmentName is not null || EmployeeName   is not null
                                                               || Manufacturer   is not null || Status         is not null;
    }
}
