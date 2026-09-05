using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.AssetTypes.Commands.CommandValidations
{
    public class CreateAssetTypeValidator : AbstractValidator<CreateAssetTypeCommandModel>
    {
        public CreateAssetTypeValidator(IAssetTypeRepository assetTypeRepository)
        {
            RuleFor(x => x.assetTypeName)
                .NotEmpty().WithMessage("Asset type name is required.")
                .MaximumLength(100)
                .MustAsync(async (name, ct) =>!await assetTypeRepository.AssetTypeNameExistsAsync(name, null, ct))
                .WithMessage(x => $"Asset type name '{x.assetTypeName}' is already in use.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }
    }
}