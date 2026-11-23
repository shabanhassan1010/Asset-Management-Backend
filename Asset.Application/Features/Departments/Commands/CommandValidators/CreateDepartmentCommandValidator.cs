using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;

namespace Asset.Application.Features.Departments.Commands.CommandValidators
{
    public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommandModel>
    {
        public CreateDepartmentCommandValidator(IDepartmentRepository departmentRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.DepartmentName)
                .NotEmpty().WithMessage(_ => localizer["Department name is required."])
                .MaximumLength(150)
                .MustAsync(async (name, ct) =>
                !await departmentRepository.IsNameExistsAsync(name, null, ct))
            .WithMessage(x => localizer["Department name '{0}' is already in use.", x.DepartmentName]);

            // Optional. Checked only when it is filled in (the index allows many NULLs).
            RuleFor(x => x.DepartmentNameAr)
                .MaximumLength(150).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 150])
                .MustAsync(async (nameAr, ct) => !await departmentRepository.IsNameArExistsAsync(nameAr!.Trim(), null, ct))
                .WithMessage(x => localizer["Arabic department name '{0}' is already in use.", x.DepartmentNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.DepartmentNameAr));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(_ => localizer["Department code is required."])
                .MaximumLength(20)
                .MustAsync(async (code, ct) =>
                    !await departmentRepository.IsCodeExistsAsync(code, null, ct))
                .WithMessage(x => localizer["Department code '{0}' is already in use.", x.Code]);
        }
    }
}
