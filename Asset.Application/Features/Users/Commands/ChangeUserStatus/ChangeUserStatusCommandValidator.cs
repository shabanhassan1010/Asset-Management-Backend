using FluentValidation;

namespace Asset.Application.Features.Users.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
{
    public ChangeUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
