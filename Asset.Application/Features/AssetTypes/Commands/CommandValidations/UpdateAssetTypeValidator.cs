using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.AssetTypes.Commands.CommandValidations
{
    public class UpdateAssetTypeValidator : AbstractValidator<UpdateAssetTypeCommandModel>
    {
        public UpdateAssetTypeValidator(IAssetTypeRepository assetTypeRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage(_ => localizer["Asset type id is required."]);

            RuleFor(x => x.assetTypeName)
            .NotEmpty().WithMessage(_ => localizer["Asset type name is required."])
            .MaximumLength(100)
            // exceptId = model.Id so the row keeping its own name isn't a duplicate.
            .MustAsync(async (model, name, ct) =>!await assetTypeRepository.AssetTypeNameExistsAsync(name, model.Id, ct))
            .WithMessage(x => localizer["Asset type name '{0}' is already in use.", x.assetTypeName]);

            // Optional. exceptId = model.Id so the row keeping its own Arabic name isn't a duplicate.
            RuleFor(x => x.assetTypeNameAr)
                .MaximumLength(100).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 100])
                .MustAsync(async (model, nameAr, ct) => !await assetTypeRepository.AssetTypeNameArExistsAsync(nameAr!.Trim(), model.Id, ct))
                .WithMessage(x => localizer["Arabic asset type name '{0}' is already in use.", x.assetTypeNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.assetTypeNameAr));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage(_ => localizer["Description must not exceed 500 characters"]);
        }
    }
}