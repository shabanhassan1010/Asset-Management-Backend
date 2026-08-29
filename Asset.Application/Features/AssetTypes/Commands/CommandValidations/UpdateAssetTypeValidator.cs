using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using FluentValidation;
namespace Asset.Application.Features.AssetTypes.Commands.CommandValidations
{
    public class UpdateAssetTypeValidator : AbstractValidator<UpdateAssetTypeCommandModel>
    {
        public UpdateAssetTypeValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid id");

            RuleFor(x => x.TypeName)
                .NotEmpty().WithMessage("Type name is required")
                .MaximumLength(100).WithMessage("Type name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }
    }
}