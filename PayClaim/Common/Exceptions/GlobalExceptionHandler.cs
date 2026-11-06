using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayClaim.DTOs.Common;

namespace PayClaim.Common.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int statusCode;
        string message;
        var errors = new List<string>();

        switch (exception)
        {
            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                message = "Resource not found.";
                errors.Add(notFoundEx.Message);
                _logger.LogWarning("Resource not found: {Message}", notFoundEx.Message);
                break;

            case ConflictException conflictEx:
                statusCode = StatusCodes.Status409Conflict;
                message = "Conflict occurred with an existing resource.";
                errors.Add(conflictEx.Message);
                _logger.LogWarning("Conflict occurred: {Message}", conflictEx.Message);
                break;

            case BusinessRuleException businessEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = "Business rule violation.";
                errors.Add(businessEx.Message);
                _logger.LogWarning("Business rule violation: {Message}", businessEx.Message);
                break;

            case ForbiddenException forbiddenEx:
                statusCode = StatusCodes.Status403Forbidden;
                message = "Forbidden access.";
                errors.Add(forbiddenEx.Message);
                _logger.LogWarning("Forbidden access: {Message}", forbiddenEx.Message);
                break;

            case UnauthorizedAccessException authEx:
                statusCode = StatusCodes.Status401Unauthorized;
                message = "Unauthorized access.";
                errors.Add(authEx.Message);
                _logger.LogWarning("Unauthorized access: {Message}", authEx.Message);
                break;

            case BadHttpRequestException badHttpEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = "Malformed request payload or syntax error.";
                errors.Add(badHttpEx.Message);
                _logger.LogWarning("Bad HTTP Request: {Message}", badHttpEx.Message);
                break;

            case DbUpdateException dbEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = "Database operation failed. Foreign key or data integrity constraint violated.";
                var dbErrorMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                errors.Add(dbErrorMsg);
                _logger.LogError(dbEx, "Database update exception occurred: {Message}", dbErrorMsg);
                break;

            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = "Invalid argument provided.";
                errors.Add(argEx.Message);
                _logger.LogWarning("Invalid argument: {Message}", argEx.Message);
                break;

            case OperationCanceledException:
                statusCode = 499; // Client Closed Request
                message = "Request was cancelled by the client.";
                _logger.LogInformation("Request was cancelled.");
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An unexpected server error occurred.";
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

                if (_env.IsDevelopment())
                {
                    errors.Add($"Exception: {exception.GetType().FullName}: {exception.Message}");
                    if (exception.InnerException != null)
                    {
                        errors.Add($"Inner Exception: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}");
                    }
                    if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                    {
                        errors.Add($"Stack Trace: {exception.StackTrace}");
                    }
                }
                else
                {
                    errors.Add($"Something went wrong. Please quote TraceId '{httpContext.TraceIdentifier}' when contacting system support.");
                }
                break;
        }

        var response = ApiResponse.Fail(message, statusCode, errors.Count > 0 ? errors : null);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
