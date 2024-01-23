// Ignore Spelling: Middleware env

using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DocManager.Utils;

/// <summary>
/// Middleware for handling exceptions during API calls.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionMiddleware> _logger = logger;
    private readonly IWebHostEnvironment _env = env;

    /// <summary>
    /// Default error message.
    /// </summary>
    public const string DEFAULT_ERROR_MESSAGE = "An internal error occurred, please contact support.";

    /// <summary>
    /// Middleware invoker.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        httpContext.Request.EnableBuffering();

        try
        {
            await _next(httpContext);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(httpContext, e);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception e)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        string method = context.Request.Method;

        var error = new HttpValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = e.GetType().Name,
            Title = DEFAULT_ERROR_MESSAGE
        };

        if (_env.IsDevelopment())
        {
            error.Errors["ErrorMessage"] = [e.Message];
            error.Errors["CallStack"] = [e.StackTrace ?? string.Empty];

            if (e.InnerException is not null)
            {
                error.Errors["InnerExceptionMessage"] = [$"{e.InnerException.GetType().Name} => {e.InnerException.Message}"];
                error.Errors["InnerExceptionCallStack"] = [e.InnerException.StackTrace ?? string.Empty];
            }
        }

        var reqId = Guid.NewGuid();
        string username = "Anonymous";
        string requestBody = string.Empty;
        string? ip = context.Connection?.RemoteIpAddress?.ToString();

        if (context.User.Identity?.IsAuthenticated ?? false)
            username = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (context.Request.Body.CanRead &&
            (method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Patch) &&
            string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 512, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(requestBody))
            requestBody = "\r\nBody: " + requestBody;

        _logger.LogError(e, "[{reqId}] Error during method call => {method} || {path} || {username} || {ip} {requestBody}",
            reqId, context.Request.Method, context.Request.Path, username, ip, requestBody);

        error.Title += $" [RequestId: {reqId}]";

        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
}