using FluentValidation;
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQueryValidator : AbstractValidator<AskAssetQuestionQuery>
    {
        public AskAssetQuestionQueryValidator()
        {
            RuleFor(x => x.Question)
                .NotEmpty()
                .WithMessage("Please type a question.")

                // The length cap is a real defence, not a formality: it bounds the
                // work every regex can be asked to do, which is the cheapest half
                // of ReDoS protection (the timeouts in the parser are the other half).
                .MaximumLength(500)
                .WithMessage("Please keep your question under 500 characters.");
        }
    }
}
