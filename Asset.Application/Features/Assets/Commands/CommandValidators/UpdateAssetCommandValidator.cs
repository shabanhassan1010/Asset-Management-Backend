using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Interfaces.Repository;
using FluentValidation;

namespace Asset.Application.Features.Assets.Commands.CommandValidators
{
    public class UpdateAssetCommandValidator : AbstractValidator<UpdateAssetCommandModel>
    {
        public UpdateAssetCommandValidator(IAssetRepository assetRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.AssetId).GreaterThan(0).WithMessage(_ => localizer["Asset id is required."]);

            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage(_ => localizer["Row version is required for update."])
                .Must(BeValidBase64).WithMessage(_ => localizer["Row version format is invalid."]);;

            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .MustAsync(async (model, serial, ct) => !await assetRepository.SerialNumberExistsAsync(serial!, model.AssetId, ct))
                .WithMessage(x => localizer["Serial number '{0}' is already in use.", x.SerialNumber])
                .When(x => !string.IsNullOrWhiteSpace(x.SerialNumber));

            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Manufacturer).MaximumLength(100);
            RuleFor(x => x.Model).MaximumLength(100);

            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage(_ => localizer["Category is required."]);


            RuleFor(x => x.Status)
                .InclusiveBetween(1, 3)
                .WithMessage(_ => localizer["Status must be Available, Assigned, or Under Maintenance."]);

            RuleFor(x => x.AssignedEmployeeId)
                .Must(id => id is not null and not 0)
                .WithMessage(_ => localizer["An employee must be assigned when status is 'Assigned'."])
                .When(x => x.Status == 2);

            RuleFor(x => x.CategoryId).GreaterThan(0);
            RuleFor(x => x.AssetTypeId).GreaterThan(0);

            RuleFor(x => x.PurchaseCost)
                .GreaterThanOrEqualTo(0).WithMessage(_ => localizer["Purchase cost cannot be negative."])
                .When(x => x.PurchaseCost.HasValue);

            RuleFor(x => x.WarrantyExpiryDate)
                .GreaterThanOrEqualTo(x => x.PurchaseDate!.Value)
                .WithMessage(_ => localizer["Warranty expiry date cannot be before the purchase date."])
                .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);
        }


        private static bool BeValidBase64(string value)
            => !string.IsNullOrWhiteSpace(value) && Convert.TryFromBase64String( value, new byte[value.Length], out _);
    }
}
