using Asset.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using FluentValidationException = FluentValidation.ValidationException;

namespace Asset.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        #region Fields
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        #endregion

        #region Constrctor
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (FluentValidationException ex)
            {
                var errors = ex.Errors.GroupBy(e => e.PropertyName)
                                      .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                await WriteAsync(context, new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed."
                });
            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogInformation("Authentication failed on {Path}.", context.Request.Path);

                await WriteAsync(context, Problem( StatusCodes.Status401Unauthorized, "Authentication failed.", ex.Message));
            }
            catch (NotFoundException ex)
            {
                await WriteAsync(context, Problem(
                    StatusCodes.Status404NotFound, "Not found.", ex.Message));
            }
            catch (BusinessException ex)
            {
                await WriteAsync(context, Problem(
                    StatusCodes.Status422UnprocessableEntity, "The request could not be completed.", ex.Message));
            }
            catch (ConflictException ex)
            {
                await WriteAsync(context, Problem(
                    StatusCodes.Status409Conflict, "Conflict.", ex.Message));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                _logger.LogWarning(ex, "Foreign key constraint violation.");

                await WriteAsync(context, Problem(StatusCodes.Status400BadRequest,"Invalid reference.","The data violates a business rule. Check the assigned employee, department, location, and status values."));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                _logger.LogWarning(ex, "Unique constraint violation while saving data");

                await WriteAsync(context, Problem(StatusCodes.Status409Conflict, "Duplicate value.", "The value you entered already exists. Please use a different value."));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict.");

                await WriteAsync(context, Problem(StatusCodes.Status409Conflict,"Concurrency conflict.","This record was modified by another user. Please reload and try again."));
            }
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning("Concurrency conflict on {Path}.", context.Request.Path);

                await WriteAsync(context, Problem(StatusCodes.Status409Conflict,"Concurrency conflict.",ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Path}.", context.Request.Path);

                await WriteAsync(context, Problem(StatusCodes.Status500InternalServerError,"An unexpected error occurred.", "Please try again. If the problem continues, quote the traceId below when reporting it."));
            }
        }

        private static ProblemDetails Problem(int status, string title, string detail) => new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        private async Task WriteAsync<TProblem>(HttpContext context, TProblem problem) where TProblem : ProblemDetails
        {

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started; the error payload could not be written.");
                return;
            }

            problem.Instance = context.Request.Path;

            // Correlates this response with the log entry above, so a user can
            // report "traceId X" and it can be found.
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(problem, options: (JsonSerializerOptions?)null, contentType: "application/problem+json");
        }
    }
}