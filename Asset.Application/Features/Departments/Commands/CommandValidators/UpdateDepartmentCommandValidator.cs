using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Departments.Commands.CommandValidators
{
    public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommandModel>
    {
        public UpdateDepartmentCommandValidator(IDepartmentRepository departmentRepository)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Department id is required.");

            RuleFor(x => x.DepartmentName)
                .NotEmpty().WithMessage("Department name is required.")
                .MaximumLength(150)
                .MustAsync(async (name, ct) =>
                !await departmentRepository.IsNameExistsAsync(name, null, ct))
            .WithMessage(x => $"Department name '{x.DepartmentName}' is already in use.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Department code is required.")
                .MaximumLength(20)
                .MustAsync(async (model, code, ct) =>
                    !await departmentRepository.IsCodeExistsAsync(code, model.Id, ct))
                .WithMessage(x => $"Department code '{x.Code}' is already in use.");
        }
    }
}