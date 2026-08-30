using FluentValidation;
using MediatR;
namespace Asset.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        #region Fields
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        #endregion

        #region Constrcutor
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
            Console.WriteLine($"[Behavior] created for {typeof(TRequest).Name}, validators = {validators.Count()}");
        }
        #endregion
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Behavior] running for {typeof(TRequest).Name}");

            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll( _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults.SelectMany(x => x.Errors).Where(x => x != null).ToList();

                if (failures.Count > 0)
                {
                    throw new ValidationException(failures);
                }
            }

            return await next();
        }
    }
}