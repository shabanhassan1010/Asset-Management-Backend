using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;

namespace Asset.Application.Features.Departments.Commands.CommandValidators
{
    public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommandModel>
    {
        public CreateDepartmentCommandValidator(IDepartmentRepository departmentRepository)
        {
            RuleFor(x => x.DepartmentName)
                .NotEmpty().WithMessage("Department name is required.")
                .MaximumLength(150)
                .MustAsync(async (name, ct) =>
                !await departmentRepository.IsNameExistsAsync(name, null, ct))
            .WithMessage(x => $"Department name '{x.DepartmentName}' is already in use.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Department code is required.")
                .MaximumLength(20)
                .MustAsync(async (code, ct) =>
                    !await departmentRepository.IsCodeExistsAsync(code, null, ct))
                .WithMessage(x => $"Department code '{x.Code}' is already in use.");
        }
    }
}
