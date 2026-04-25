using System.Net;
using BuildingBlocks.Application.Exceptions;

namespace CrmSystem.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        object response = new { title = "An unexpected error occurred" };

        switch (exception)
        {
            case ApplicationValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                response = new { title = "Validation failed", errors = validationException.Errors };
                break;
            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                response = new { title = exception.Message };
                break;
            case ConflictException:
                statusCode = HttpStatusCode.Conflict;
                response = new { title = exception.Message };
                break;
            case UnauthorizedException:
                statusCode = HttpStatusCode.Unauthorized;
                response = new { title = exception.Message };
                break;
            case ForbiddenException:
                statusCode = HttpStatusCode.Forbidden;
                response = new { title = exception.Message };
                break;
        }

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}
