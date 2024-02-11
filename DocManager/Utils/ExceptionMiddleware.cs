// Ignore Spelling: Middleware env

using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace DocManager.Utils;

/// <summary>
/// Middleware for handling exceptions during API calls.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env, IConfiguration config)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger _logger = logger;
    private readonly IWebHostEnvironment _env = env;
    private readonly IConfiguration _config = config;

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

        var requestId = Guid.NewGuid();
        httpContext.Items[RequestContext.REQUEST_ID] = requestId;

        try
        {
            await _next(httpContext);

            int statusCode = httpContext.Response.StatusCode;

            if ((statusCode is StatusCodes.Status403Forbidden or StatusCodes.Status401Unauthorized) && CanLogUnauthorizedRequests())
                await HandleUnauthorizedRequest(httpContext);

            if ((statusCode is StatusCodes.Status400BadRequest or >= StatusCodes.Status404NotFound) && CanLogBadRequests())
                await HandleBadRequest(httpContext);
        }
        catch (Exception e)
        {
            await HandleException(httpContext, e);
        }
    }

    private async ValueTask HandleException(HttpContext context, Exception e)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

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

        var info = await RequestContext.GetRequestContextInfo(context);

        string requestBody = info.RequestBody;
        if (!string.IsNullOrWhiteSpace(requestBody))
            requestBody = "\r\nBody: " + requestBody;

        _logger.LogError(e, "[{requestId}] Error during method call => {method} || {path} || {controlerName} || {username} || {ip} {requestBody}",
            info.RequestId, info.Method, info.Path, info.ControllerName, info.Username, info.IP, requestBody);

        error.Title += $" [RequestId: {info.RequestId}]";

        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }

    private async ValueTask HandleUnauthorizedRequest(HttpContext context)
    {
        var info = await RequestContext.GetRequestContextInfo(context, false);
        string controllerName = info.ControllerName;
        if (string.IsNullOrEmpty(controllerName))
            controllerName = GetType().FullName!;

        string responseType = context.Response.StatusCode == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden";

        _logger.LogInformation("[{requestId}] {responseType} request => {method} || {path} || {controllerName} || {username} || {ip}",
            info.RequestId, responseType, info.Method, info.Path, controllerName, info.Username, info.IP);
    }

    private bool CanLogUnauthorizedRequests()
    {
        if (!bool.TryParse(_config["LogUnauthorizedRequests"] ?? string.Empty, out bool logEnabled))
            logEnabled = false;

        return logEnabled;
    }

    private async ValueTask HandleBadRequest(HttpContext context)
    {
        var info = await RequestContext.GetRequestContextInfo(context);

        string controllerName = info.ControllerName;
        if (string.IsNullOrEmpty(controllerName))
            controllerName = GetType().FullName!;

        string requestBody = info.RequestBody;
        if (!string.IsNullOrWhiteSpace(requestBody))
            requestBody = "\r\nRequest Body: " + requestBody;

        string reasonPhrase = ReasonPhrases.GetReasonPhrase(context.Response.StatusCode);

        string responseBody = string.Empty;

        if (context.Items.ContainsKey("BadRequestResult"))
        {
            object? badRequestResult = context.Items["BadRequestResult"];
            if (badRequestResult is not null)
            {
                responseBody = badRequestResult is HttpValidationProblemDetails errorDetails && errorDetails.Errors.Any()
                    ? "\r\nResponse: " + JsonSerializer.Serialize(errorDetails.Errors)
                    : "\r\nResponse: " + JsonSerializer.Serialize(badRequestResult);
            }
        }

        _logger.LogInformation("[{requestId}] Result was {reasonPhrase} => {method} || {path} || {controllerName} || {username} || {ip} {requestBody} {responseBody}",
            info.RequestId, reasonPhrase, info.Method, info.Path, controllerName, info.Username, info.IP, requestBody, responseBody);
    }

    private bool CanLogBadRequests()
    {
        if (!bool.TryParse(_config["LogBadRequests"] ?? string.Empty, out bool logEnabled))
            logEnabled = false;

        return logEnabled;
    }
}

/// <summary>
/// Implements a filter to capture <see cref="BadRequestObjectResult"/> emitted by Asp.Net Core.
/// </summary>
public class BadRequestLoggerFilter : IAsyncResultFilter
{
    /// <inheritdoc/>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();

        if (context.Result is BadRequestObjectResult result)
            context.HttpContext.Items["BadRequestResult"] = result.Value;
    }
}