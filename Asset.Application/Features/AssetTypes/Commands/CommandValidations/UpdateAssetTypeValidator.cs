using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.AssetTypes.Commands.CommandValidations
{
    public class UpdateAssetTypeValidator : AbstractValidator<UpdateAssetTypeCommandModel>
    {
        public UpdateAssetTypeValidator(IAssetTypeRepository assetTypeRepository)
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid id");

            RuleFor(x => x.assetTypeName)
            .NotEmpty().WithMessage("Asset type name is required.")
            .MaximumLength(100)
            // exceptId = model.Id so the row keeping its own name isn't a duplicate.
            .MustAsync(async (model, name, ct) =>!await assetTypeRepository.AssetTypeNameExistsAsync(name, model.Id, ct))
            .WithMessage(x => $"Asset type name '{x.assetTypeName}' is already in use.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }
    }
}