using Asset.Application.Common.Responses;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Asset.API.Extensions
{
    public static class RateLimitingExtensions
    {
        // Referenced by [EnableRateLimiting] on the controller.
        // A constant, because a typo in the string would silently disable the limit.
        public const string AiPolicy = "ai-per-user";

        public static IServiceCollection AddAiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy(AiPolicy, httpContext =>
                {
                    // Partition by user id, not by IP address (R4.7 asks for per user).
                    // IP would punish everyone behind one office NAT for the actions
                    // of a single person, and would let one person bypass the limit
                    // simply by switching networks.
                    var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                       ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                       ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),

                            // No queue. A question that has to wait is worse than a
                            // clear "slow down" - the person is sitting in front of a
                            // chat box watching a spinner.
                            QueueLimit = 0
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Without this, a throttled request returns an empty body and the chat
                // shows nothing at all. R4.5 wants graceful behaviour for rate limits,
                // which means a sentence the person can read.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString();
                    }

                    var body = new ApiResponse<object>
                    {
                        Success = false,
                        Message = "You're sending questions a bit too quickly. Please wait a moment and try again."
                    };

                    await context.HttpContext.Response.WriteAsync(
                        JsonSerializer.Serialize(body,
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        cancellationToken);
                };
            });

            return services;
        }
    }
}
