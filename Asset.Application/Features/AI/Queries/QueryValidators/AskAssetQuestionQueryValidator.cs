using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Features.AI.Queries.QueryModel;
using FluentValidation;
namespace Asset.Application.Features.AI.Queries.QueryValidators
{
    public class AskAssetQuestionQueryValidator : AbstractValidator<AskAssetQuestionQuery>
    {
        public AskAssetQuestionQueryValidator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(x => x.Question)
                .NotEmpty().WithMessage(_ => localizer["Please type a question."])
                .MaximumLength(500).WithMessage(_ => localizer["Please keep your question under 500 characters."]);
        }
    }
}