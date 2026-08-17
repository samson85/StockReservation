using System.Diagnostics;

namespace StockReservation.Api;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();

        await next(context);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0} ms. TraceId {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMs,
            context.TraceIdentifier);
    }
}
