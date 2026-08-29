using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using FluentValidation;
namespace Asset.Application.Features.AssetTypes.Commands.CommandValidations
{
    public class CreateAssetTypeValidator : AbstractValidator<CreateAssetTypeCommandModel>
    {
        public CreateAssetTypeValidator()
        {
            RuleFor(x => x.TypeName)
                .NotEmpty().WithMessage("Type name is required")
                .MaximumLength(100).WithMessage("Type name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }
    }
}