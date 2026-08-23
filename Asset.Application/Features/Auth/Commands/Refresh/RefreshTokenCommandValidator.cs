#region
using FluentValidation;
#endregion

namespace Asset.Application.Features.Auth.Commands.Refresh;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        // 200 is the column width configured on the entity.
        // Rejecting longer input here stops a truncation error surfacing as a 500.
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
