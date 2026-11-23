using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Category.Commands.CommandValidators
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommandModel>
    {
        public CreateCategoryCommandValidator(ICategoryRepository categoryRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage(_ => localizer["Category name is required."])
                .MaximumLength(100)
                .MustAsync(async (name, ct) => !await categoryRepository.CategoryNameExistsAsync(name, null, ct))
                .WithMessage(x => localizer["Category name '{0}' is already in use.", x.CategoryName]);

            // Optional. Checked only when it is filled in (the index allows many NULLs).
            RuleFor(x => x.CategoryNameAr)
                .MaximumLength(100).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 100])
                .MustAsync(async (nameAr, ct) => !await categoryRepository.CategoryNameArExistsAsync(nameAr!.Trim(), null, ct))
                .WithMessage(x => localizer["Arabic category name '{0}' is already in use.", x.CategoryNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.CategoryNameAr));

            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
