using System.Net;
using System.Text.Json;
using StockReservation.Domain;

namespace StockReservation.Api;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(
                "Business rule rejected request {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await WriteProblemAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (NotFoundDomainException ex)
        {
            logger.LogInformation(
                "Resource not found for request {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await WriteProblemAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was cancelled by the client: {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled request exception. TraceId {TraceId}, {Method} {Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode status,
        string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        var body = new
        {
            type = $"https://httpstatuses.com/{(int)status}",
            title = status.ToString(),
            status = (int)status,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
