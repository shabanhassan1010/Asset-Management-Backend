using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Category.Commands.CommandValidators
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommandModel>
    {
        public CreateCategoryCommandValidator(ICategoryRepository categoryRepository)
        {
            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100)
                .MustAsync(async (name, ct) => !await categoryRepository.CategoryNameExistsAsync(name, null, ct))
                .WithMessage(x => $"Category name '{x.CategoryName}' is already in use.");

            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
