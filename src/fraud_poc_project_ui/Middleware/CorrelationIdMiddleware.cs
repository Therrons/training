using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace fraud_poc_project.Middleware
{
    /// <summary>
    /// Middleware for adding and propagating correlation IDs across the request pipeline.
    /// Enables distributed tracing of requests through multiple services.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation ID from request header, otherwise generate new one
            var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdValue)
                ? correlationIdValue.ToString()
                : Guid.NewGuid().ToString();

            // Store in HttpContext items for access throughout request
            context.Items[CorrelationIdHeaderName] = correlationId;

            // Add to response headers
            context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

            // Add to log context for structured logging
            using (_logger.BeginScope(new { CorrelationId = correlationId }))
            {
                try
                {
                    await _next(context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred. CorrelationId: {CorrelationId}", correlationId);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Extension method to register CorrelationIdMiddleware in the pipeline.
    /// </summary>
    public static class CorrelationIdExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }

    /// <summary>
    /// Helper class to access correlation ID from current request context.
    /// </summary>
    public static class CorrelationIdAccessor
    {
        /// <summary>
        /// Gets the correlation ID from HTTP context items.
        /// </summary>
        public static string GetCorrelationId(this HttpContext context)
        {
            return context?.Items.TryGetValue("X-Correlation-ID", out var correlationId) == true
                ? correlationId?.ToString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();
        }
    }
}
