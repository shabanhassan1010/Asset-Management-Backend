using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Locations.Commands.CommandValidator
{
    public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommandModel>
    {
        public UpdateLocationCommandValidator(ILocationRepository locationRepository)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Location id is required.");

            RuleFor(x => x.LocationName)
                .NotEmpty().WithMessage("Location name is required.")
                .MaximumLength(150)
                .MustAsync(async (model, name, ct) =>
                    !await locationRepository.LocationNameExistsAsync(name, model.Id, ct))
                .WithMessage(x => $"Location name '{x.LocationName}' is already in use.");

            RuleFor(x => x.Address).MaximumLength(300);
        }
    }
}
