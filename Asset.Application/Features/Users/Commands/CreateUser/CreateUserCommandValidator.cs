using FluentValidation;

namespace Asset.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().MinimumLength(3).MaximumLength(256)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may only contain letters, digits, dot, underscore or hyphen.");

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.EmployeeId)
                                .GreaterThan(0)
                                .WithMessage("A user must be linked to an employee.");
        // Mirrors the Identity password options configured in DI, so the client
        // gets one clean 400 instead of Identity's error list in another shape.
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);

        // No rule for Role. The enum only has two values, so "SuperAdmin" fails
        // at model binding and never reaches a validator - a rule here would
        // just be a second copy of what the type already says.

    }
}
