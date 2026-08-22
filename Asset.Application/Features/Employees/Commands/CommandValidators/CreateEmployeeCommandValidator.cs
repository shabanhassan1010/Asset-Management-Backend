using Asset.Application.Features.Employees.Commands.CommandModels;
using FluentValidation;
namespace Asset.Application.Features.Employees.Commands.CommandValidators
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommandModel>
    {
        public CreateEmployeeCommandValidator()
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage("Employee code is required.")
                .MaximumLength(30).WithMessage("Employee code must not exceed 30 characters.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Employee name is required.")
                .MaximumLength(200).WithMessage("Employee name must not exceed 200 characters.");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Department is required.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is not valid.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(x => x.Phone)
                .MaximumLength(30)
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        }
    }
}
