using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Asset.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        #region Fields
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        #endregion

        #region Constructor
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        #endregion

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;  // Get the name of class like [GetAllAssetsQuery - CreateAssetCommand]

            _logger.LogInformation("Start Handle For {RequestName}", requestName);

            var stopwatch = Stopwatch.StartNew();
            // he will go to the next behavior or handler,
            // each line before this line he will exectue before handler 
            // and each line after it will exectue after the handler finish
            var response = await next();      
            stopwatch.Stop();   // see how handler take time?

            _logger.LogInformation("End Handle For {RequestName} in {ElapsedMs} ms", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
    }
}