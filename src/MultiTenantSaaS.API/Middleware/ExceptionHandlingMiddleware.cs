using System.Net;
using System.Text.Json;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Models;

namespace MultiTenantSaaS.API.Middleware;

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
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Data = null
        };

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = validationException.Message;
                response.Errors = validationException.Failures
                    .SelectMany(f => f.Value.Select(msg => $"{f.Key}: {msg}"))
                    .ToList();
                break;

            case NotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = notFoundException.Message;
                response.Errors = notFoundException.Errors ?? new List<string> { notFoundException.Message };
                break;

            case UnauthorizedException unauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = unauthorizedException.Message;
                response.Errors = unauthorizedException.Errors ?? new List<string> { unauthorizedException.Message };
                break;

            case ForbiddenException forbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Message = forbiddenException.Message;
                response.Errors = forbiddenException.Errors ?? new List<string> { forbiddenException.Message };
                break;

            case ApiException apiException:
                context.Response.StatusCode = apiException.StatusCode;
                response.Message = apiException.Message;
                response.Errors = apiException.Errors ?? new List<string> { apiException.Message };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred on the server.";
                response.Errors = new List<string> { exception.Message };
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
