using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Locations.Commands.CommandValidator
{
    public class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommandModel>
    {
        public CreateLocationCommandValidator(ILocationRepository locationRepository)
        {
            RuleFor(x => x.LocationName)
                .NotEmpty().WithMessage("Location name is required.")
                .MaximumLength(150)
                .MustAsync(async (name, ct) =>
                    !await locationRepository.LocationNameExistsAsync(name, null, ct))
                .WithMessage(x => $"Location name '{x.LocationName}' is already in use.");

            RuleFor(x => x.Address).MaximumLength(300);
        }
    }
}
