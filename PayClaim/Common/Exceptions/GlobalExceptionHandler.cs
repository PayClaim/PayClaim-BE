using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PayClaim.Common.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {

            var (statusCode, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict occurred"),
                BusinessRuleException => (StatusCodes.Status400BadRequest, "Business rule violation"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "An unexpected error occurred.");
            }
            else
            {
                _logger.LogWarning(
                    "Request failed with status {StatusCode}: {Message}",
                    statusCode,
                    exception.Message
                );
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                 Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "Something went wrong. Please try again later."
                : exception.Message
            };

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
