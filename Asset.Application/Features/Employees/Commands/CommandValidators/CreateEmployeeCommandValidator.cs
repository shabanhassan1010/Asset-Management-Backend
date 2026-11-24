using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Employees.Commands.CommandModels;
using FluentValidation;
namespace Asset.Application.Features.Employees.Commands.CommandValidators
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommandModel>
    {
        public CreateEmployeeCommandValidator(IStringLocalizer<SharedResources> localizer)
        {

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage(_ => localizer["Employee name is required."])
                .MaximumLength(200).WithMessage(_ => localizer["Employee name must not exceed 200 characters."]);

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage(_ => localizer["Department is required."]);

            // Optional. No "already in use" check: two employees can share a name.
            RuleFor(x => x.FullNameAr)
                .MaximumLength(200).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 200])
                .When(x => !string.IsNullOrWhiteSpace(x.FullNameAr));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_ => localizer["Email is required."])
                .EmailAddress().WithMessage(_ => localizer["Email format is not valid."])
                .MaximumLength(256).WithMessage(_ => localizer["Email must not exceed 256 characters."]);

            RuleFor(x => x.Phone)
                .MaximumLength(30)
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        }
    }
}
