using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Interfaces.IRepository;
using FluentValidation;
namespace Asset.Application.Features.Locations.Commands.CommandValidator
{
    public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommandModel>
    {
        public UpdateLocationCommandValidator(ILocationRepository locationRepository, IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(_ => localizer["Location id is required."]);

            RuleFor(x => x.LocationName)
                .NotEmpty().WithMessage(_ => localizer["Location name is required."])
                .MaximumLength(150)
                .MustAsync(async (model, name, ct) =>
                    !await locationRepository.LocationNameExistsAsync(name, model.Id, ct))
                .WithMessage(x => localizer["Location name '{0}' is already in use.", x.LocationName]);

            // Optional. exceptId = model.Id so the row keeping its own Arabic name isn't a duplicate.
            RuleFor(x => x.LocationNameAr)
                .MaximumLength(150).WithMessage(_ => localizer["Arabic name must not exceed {0} characters.", 150])
                .MustAsync(async (model, nameAr, ct) => !await locationRepository.LocationNameArExistsAsync(nameAr!.Trim(), model.Id, ct))
                .WithMessage(x => localizer["Arabic location name '{0}' is already in use.", x.LocationNameAr!])
                .When(x => !string.IsNullOrWhiteSpace(x.LocationNameAr));

            RuleFor(x => x.Address).MaximumLength(300);
        }
    }
}
