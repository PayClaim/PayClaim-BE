using System.Security.Claims;
using PayClaim.Middlewares;
using Serilog;
using Serilog.Events;

namespace PayClaim.Extensions;

public static class LoggingExtensions
{
    /// <summary>
    /// Mengonfigurasi Serilog sebagai logging provider utama dengan format terstruktur.
    /// </summary>
    public static WebApplicationBuilder AddPayClaimSerilog(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);
        }

        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName));

        builder.Services.AddSingleton<RequestTimingMiddleware>();

        return builder;
    }

    /// <summary>
    /// Mencatat durasi/kecepatan eksekusi setiap endpoint dalam milidetik (ms) beserta status code dan correlation id.
    /// </summary>
    public static IApplicationBuilder UseEndpointTimingLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestTimingMiddleware>();

        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.GetLevel = (httpContext, elapsedMs, ex) =>
                ex != null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("EmployeeId")
                    ?? "anonymous";

                diagnosticContext.Set("UserId", userId);

                if (httpContext.Response.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId.ToString());
                }
            };
        });
    }
}
