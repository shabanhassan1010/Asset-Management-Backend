using Asset.Application.Features.Assets.Commands.CommandModels;
using FluentValidation;
namespace Asset.Application.Features.Assets.Commands.CommandValidators
{
    public class RetireAssetValidator : AbstractValidator<RetireAssetCommandModel>
    {
        public RetireAssetValidator()
        {
            RuleFor(x => x.AssetId).GreaterThan(0);

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A retirement reason is required.")
                .MaximumLength(500);

            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage("The record version is required.")
                .Must(BeValidBase64).WithMessage("The record version is not valid.");
        }
        private static bool BeValidBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Convert.TryFromBase64String(value, new byte[value.Length], out _);
        }
    }
}
