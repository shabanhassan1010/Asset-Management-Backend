using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.AssetTypes.Commands.CommandValidations
{
    public class CreateAssetTypeValidator : AbstractValidator<CreateAssetTypeCommandModel>
    {
        public CreateAssetTypeValidator(IAssetTypeRepository assetTypeRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.assetTypeName)
                .NotEmpty().WithMessage(_ => localizer["Asset type name is required."])
                .MaximumLength(100)
                .MustAsync(async (name, ct) =>!await assetTypeRepository.AssetTypeNameExistsAsync(name, null, ct))
                .WithMessage(x => localizer["Asset type name '{0}' is already in use.", x.assetTypeName]);

            // Optional. Checked only when it is filled in (the index allows many NULLs).
            RuleFor(x => x.assetTypeNameAr)
                .MaximumLength(100).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 100])
                .MustAsync(async (nameAr, ct) => !await assetTypeRepository.AssetTypeNameArExistsAsync(nameAr!.Trim(), null, ct))
                .WithMessage(x => localizer["Arabic asset type name '{0}' is already in use.", x.assetTypeNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.assetTypeNameAr));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage(_ => localizer["Description must not exceed 500 characters"]);
        }
    }
}