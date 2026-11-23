#region
using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Interfaces.Repository;
using FluentValidation;
#endregion

namespace Asset.Application.Features.Assets.Commands.CommandValidators
{
    public class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommandModel>
    {
        public CreateAssetCommandValidator(IAssetRepository assetRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.AssetCode)
                .NotEmpty().WithMessage(_ => localizer["Asset code is required."])
                .MaximumLength(50)
                .MustAsync((code, ct) => IsUniqueAsync(assetRepository.IsCodeExistsAsync, code, ct))
                .WithMessage(x => localizer["Asset code '{0}' is already in use.", x.AssetCode]);

            RuleFor(x => x.AssetName)
                .NotEmpty().WithMessage(_ => localizer["Asset name is required."])
                .MaximumLength(200)
                .MustAsync((name, ct) => IsUniqueAsync(assetRepository.IsNameExistsAsync, name, ct))
                .WithMessage(x => localizer["Asset name '{0}' is already in use.", x.AssetName]);

            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .MustAsync((serial, ct) => IsUniqueAsync(assetRepository.SerialNumberExistsAsync, serial!, ct))
                .WithMessage(x => localizer["Serial number '{0}' is already in use.", x.SerialNumber])
                .When(x => !string.IsNullOrWhiteSpace(x.SerialNumber));

            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage(_ => localizer["Category is required."]);
            RuleFor(x => x.AssetTypeId).GreaterThan(0).WithMessage(_ => localizer["Type is required."]);

            // 1=Available, 2=Assigned, 3=UnderMaintenance — 4=Retired is deliberately
            // excluded here: the UI note says Retired only happens via the Retire action.
            RuleFor(x => x.Status)
                .InclusiveBetween(1, 3)
                .WithMessage(_ => localizer["Status must be Available, Assigned, or Under Maintenance."]);

            RuleFor(x => x.AssignedEmployeeId)
                .NotNull()
                .WithMessage(_ => localizer["An employee must be assigned when status is 'Assigned'."])
                .When(x => x.Status == 2);

            RuleFor(x => x.LocationId)
                .NotEmpty().WithMessage(_ => localizer["A location is required: every asset must have a location."]);

            RuleFor(x => x.WarrantyExpiryDate)
                .GreaterThanOrEqualTo(x => x.PurchaseDate!.Value)
                .WithMessage(_ => localizer["Warranty expiry date cannot be before the purchase date."])
                .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);
        }

        // All three uniqueness rules follow the same "does this value already exist,
        // excluding no id since this is Create" shape — one helper instead of
        // repeating the same async lambda three times.
        private static Task<bool> IsUniqueAsync(Func<string, int?, CancellationToken, Task<bool>> existsCheck, string value, CancellationToken ct)
        {
            return existsCheck(value, null, ct).ContinueWith(t => !t.Result, ct);
        }
    }
}
