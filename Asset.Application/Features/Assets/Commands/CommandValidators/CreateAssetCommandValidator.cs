using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Interfaces.Repository;
using FluentValidation;
namespace Asset.Application.Features.Assets.Commands.CommandValidators
{
    public class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommandModel>
    {
        public CreateAssetCommandValidator(IAssetRepository assetRepository)
        {
            RuleFor(x => x.AssetCode)
                .NotEmpty().WithMessage("Asset code is required.")
                .MaximumLength(50)
                .MustAsync((code, ct) => IsUniqueAsync(assetRepository.IsCodeExistsAsync, code, ct))
                .WithMessage(x => $"Asset code '{x.AssetCode}' is already in use.");

            RuleFor(x => x.AssetName)
                .NotEmpty().WithMessage("Asset name is required.")
                .MaximumLength(200)
                .MustAsync((name, ct) => IsUniqueAsync(assetRepository.IsNameExistsAsync, name, ct))
                .WithMessage(x => $"Asset name '{x.AssetName}' is already in use.");

            // SerialNumber is nullable + your DB index is a *filtered* unique index
            // (unique only when NOT NULL) — so only check when a value was actually supplied.
            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .MustAsync((serial, ct) => IsUniqueAsync(assetRepository.SerialNumberExistsAsync, serial!, ct))
                .WithMessage(x => $"Serial number '{x.SerialNumber}' is already in use.")
                .When(x => !string.IsNullOrWhiteSpace(x.SerialNumber));

            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Category is required.");
            RuleFor(x => x.AssetTypeId).GreaterThan(0).WithMessage("Type is required.");

            // 1=Available, 2=Assigned, 3=UnderMaintenance — 4=Retired is deliberately
            // excluded here: the UI note says Retired only happens via the Retire action.
            RuleFor(x => x.Status)
                .InclusiveBetween(1, 3)
                .WithMessage("Status must be Available, Assigned, or Under Maintenance.");

            RuleFor(x => x.AssignedEmployeeId)
                .NotNull()
                .WithMessage("An employee must be assigned when status is 'Assigned'.")
                .When(x => x.Status == 2);

            RuleFor(x => x.WarrantyExpiryDate)
                .GreaterThanOrEqualTo(x => x.PurchaseDate!.Value)
                .WithMessage("Warranty expiry date cannot be before the purchase date.")
                .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);
        }

        // All three uniqueness rules follow the same "does this value already exist,
        // excluding no id since this is Create" shape — one helper instead of
        // repeating the same async lambda three times.
        private static Task<bool> IsUniqueAsync(Func<string, int?, CancellationToken, Task<bool>> existsCheck, string value, CancellationToken ct)
            => existsCheck(value, null, ct).ContinueWith(t => !t.Result, ct);
    }
}
