namespace OneCompetitions.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Unauthorized request rejected");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", detail = exception.Message, status = 401 });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Invalid request rejected");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { title = "Bad Request", detail = exception.Message, status = 400 });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request failure");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { title = "Internal Server Error", detail = "The request could not be completed.", status = 500 });
        }
    }
}
