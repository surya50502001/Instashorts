using System.Net;
using System.Text.Json;
using FluentValidation;

namespace ScrollGuardian.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException valEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Validation failed.";
                response.Errors = valEx.Errors.Select(e => e.ErrorMessage).ToList();
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access.";
                break;

            case KeyNotFoundException knf:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = knf.Message;
                break;

            case InvalidOperationException inv:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = inv.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred. Please try again later.";
                break;
        }

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
