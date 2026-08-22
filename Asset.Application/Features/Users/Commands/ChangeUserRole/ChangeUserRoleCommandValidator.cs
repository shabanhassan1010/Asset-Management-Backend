using FluentValidation;

namespace Asset.Application.Features.Users.Commands.ChangeUserRole;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        // Role needs no rule - the enum is the constraint.
        RuleFor(x => x.UserId).NotEmpty();
    }
}
