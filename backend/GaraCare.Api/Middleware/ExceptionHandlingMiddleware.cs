using System.Net;
using System.Text.Json;
using GaraCare.Application.Exceptions;

namespace GaraCare.Api.Middleware;

// Maps business exceptions to the HTTP status codes documented in docs/04-api-contract.md.
public class ExceptionHandlingMiddleware
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
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                InvalidTransitionException => HttpStatusCode.BadRequest,
                ForbiddenActionException => HttpStatusCode.Forbidden,
                EntityNotFoundException => HttpStatusCode.NotFound,
                BusinessException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }
}
