using FluentValidation;

namespace Asset.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);

        // The ceiling is the point of validating page size at all: without it a
        // caller can ask for pageSize=1000000 and turn paging back into a full
        // table scan.
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Search).MaximumLength(256);
    }
}
