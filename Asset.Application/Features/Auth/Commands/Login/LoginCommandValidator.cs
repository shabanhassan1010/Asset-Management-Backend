using FluentValidation;

namespace Asset.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        // Shape only. Whether the credentials are CORRECT is the handler's job -
        // a validator that touched the database would leak which usernames exist.
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}
