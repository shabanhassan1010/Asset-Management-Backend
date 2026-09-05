using Asset.Application.Features.AI.Queries.QueryModel;
using FluentValidation;
namespace Asset.Application.Features.AI.Queries.QueryValidators
{
    public class AskAssetQuestionQueryValidator : AbstractValidator<AskAssetQuestionQuery>
    {
        public AskAssetQuestionQueryValidator()
        {
            RuleFor(x => x.Question)
                .NotEmpty().WithMessage("Please type a question.")
                .MaximumLength(500).WithMessage("Please keep your question under 500 characters.");
        }
    }
}