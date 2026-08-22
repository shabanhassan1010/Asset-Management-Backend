using Asset.Application.Features.AssetTransfers.CommandModel;
using FluentValidation;
namespace Asset.Application.Features.AssetTransfers.CommandValidator
{
    public class TransferAssetCommandValidator : AbstractValidator<TransferAssetCommandModel>
    {
        public TransferAssetCommandValidator()
        {
            RuleFor(x => x.AssetId).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason is required for every transfer.")
                .MaximumLength(500);

            RuleFor(x => x.TransferDate)
                .NotEmpty().WithMessage("A transfer date is required.");

            RuleFor(x => x.ToEmployeeId).GreaterThanOrEqualTo(0).When(x => x.ToEmployeeId.HasValue);
            RuleFor(x => x.ToDepartmentId).GreaterThanOrEqualTo(0).When(x => x.ToDepartmentId.HasValue);
            RuleFor(x => x.ToLocationId).GreaterThanOrEqualTo(0).When(x => x.ToLocationId.HasValue);

            // An employee always belongs to a department, so assigning an asset to a person without naming the department would leave the two
            // columns contradicting each other. Caught here rather than in the
            // handler because it needs nothing from the database.
            RuleFor(x => x.ToDepartmentId)
                .NotNull()
                .When(x => x.ToEmployeeId is > 0)
                .WithMessage("A transfer to an employee also needs a department.");

            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage("RowVersion is required. Re-read the asset and try again.")
                .Must(BeBase64).WithMessage("RowVersion is not a valid Base64 value.");
        }

        // Checked here so a malformed stamp is a 400 rather than a
        // FormatException from Convert.FromBase64String, which would surface
        // as a 500.
        private static bool BeBase64(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Convert.TryFromBase64String(value, new byte[value.Length], out _);
        }
    }
}
