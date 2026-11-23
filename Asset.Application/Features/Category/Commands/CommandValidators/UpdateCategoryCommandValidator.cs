using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;

namespace Asset.Application.Features.Category.Commands.CommandValidators
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommandModel>
    {
        public UpdateCategoryCommandValidator(ICategoryRepository categoryRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(_ => localizer["Category id is required."]);

            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage(_ => localizer["Category name is required."])
                .MaximumLength(100)
                // exceptId = model.Id => to check if Category name exists before or not
                .MustAsync(async (model, name, ct) =>
                    !await categoryRepository.CategoryNameExistsAsync(name, model.Id, ct))
                .WithMessage(x => localizer["Category name '{0}' is already in use.", x.CategoryName]);

            // Optional. exceptId = model.Id so the row keeping its own Arabic name isn't a duplicate.
            RuleFor(x => x.CategoryNameAr)
                .MaximumLength(100).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 100])
                .MustAsync(async (model, nameAr, ct) => !await categoryRepository.CategoryNameArExistsAsync(nameAr!.Trim(), model.Id, ct))
                .WithMessage(x => localizer["Arabic category name '{0}' is already in use.", x.CategoryNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.CategoryNameAr));

            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
