using FluentValidation;
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
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