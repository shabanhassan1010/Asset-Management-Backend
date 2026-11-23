using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Departments.Commands.CommandValidators
{
    public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommandModel>
    {
        public UpdateDepartmentCommandValidator(IDepartmentRepository departmentRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(_ => localizer["Department id is required."]);

            RuleFor(x => x.DepartmentName)
                .NotEmpty().WithMessage(_ => localizer["Department name is required."])
                .MaximumLength(150)
                .MustAsync(async (model,name, ct) =>
                !await departmentRepository.IsNameExistsAsync(name, model.Id, ct))
            .WithMessage(x => localizer["Department name '{0}' is already in use.", x.DepartmentName]);

            // Optional. exceptId = model.Id so the row keeping its own Arabic name isn't a duplicate.
            RuleFor(x => x.DepartmentNameAr)
                .MaximumLength(150).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 150])
                .MustAsync(async (model, nameAr, ct) => !await departmentRepository.IsNameArExistsAsync(nameAr!.Trim(), model.Id, ct))
                .WithMessage(x => localizer["Arabic department name '{0}' is already in use.", x.DepartmentNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.DepartmentNameAr));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(_ => localizer["Department code is required."])
                .MaximumLength(20)
                .MustAsync(async (model, code, ct) =>
                    !await departmentRepository.IsCodeExistsAsync(code, model.Id, ct))
                .WithMessage(x => localizer["Department code '{0}' is already in use.", x.Code]);
        }
    }
}