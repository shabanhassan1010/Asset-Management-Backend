using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.AssetTransfers.Commands.CommandModel;
using FluentValidation;
namespace Asset.Application.Features.AssetTransfers.Commands.CommandValidator
{
    public class TransferAssetCommandValidator : AbstractValidator<TransferAssetCommandModel>
    {
        public TransferAssetCommandValidator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.AssetId).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage(_ => localizer["A reason is required for every transfer."])
                .MaximumLength(500);

            RuleFor(x => x.TransferDate)
                .NotEmpty().WithMessage(_ => localizer["A transfer date is required."]);

            RuleFor(x => x.ToEmployeeId).GreaterThanOrEqualTo(0).When(x => x.ToEmployeeId.HasValue);
            RuleFor(x => x.ToDepartmentId).GreaterThanOrEqualTo(0).When(x => x.ToDepartmentId.HasValue);
            RuleFor(x => x.ToLocationId).GreaterThanOrEqualTo(0).When(x => x.ToLocationId.HasValue);


            RuleFor(x => x.ToDepartmentId).NotNull()
                .When(x => x.ToEmployeeId is > 0)
                .WithMessage(_ => localizer["A transfer to an employee also needs a department."]);

            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage(_ => localizer["RowVersion is required. Re-read the asset and try again."])
                .Must(BeBase64).WithMessage(_ => localizer["RowVersion is not a valid Base64 value."]);
        }


        private static bool BeBase64(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Convert.TryFromBase64String(value, new byte[value.Length], out _);
        }
    }
}
