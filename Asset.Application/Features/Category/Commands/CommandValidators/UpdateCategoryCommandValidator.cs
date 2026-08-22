using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;

namespace Asset.Application.Features.Category.Commands.CommandValidators
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommandModel>
    {
        public UpdateCategoryCommandValidator(ICategoryRepository categoryRepository)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Category id is required.");

            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100)
                // exceptId = model.Id => to check if Category name exists before or not
                .MustAsync(async (model, name, ct) =>
                    !await categoryRepository.CategoryNameExistsAsync(name, model.Id, ct))
                .WithMessage(x => $"Category name '{x.CategoryName}' is already in use.");

            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
