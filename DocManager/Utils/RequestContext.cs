using System.Security.Claims;
using System.Text;

using DocManager.Exceptions;

using Microsoft.AspNetCore.Mvc.Controllers;

namespace DocManager.Utils;

/// <summary>
/// Helper class that provides access to useful context information related to a request being currently by a Controller.
/// </summary>
public class RequestContext(IHttpContextAccessor accessor)
{
    private readonly IHttpContextAccessor _accessor = accessor;

    /// <summary>
    /// Name of the key for RequestId in <see cref="HttpContext.Items"/>.
    /// </summary>
    public const string REQUEST_ID = "RequestId";

    /// <summary>
    /// Returns the Request Id associated with the current HTTP Request being processed by the controller.
    /// </summary>
    public Guid GetRequestId() => GetRequestId(_accessor.HttpContext);

    /// <summary>
    /// Returns the Request Id associated with the current HTTP Request being processed by the controller.
    /// </summary>
    public static Guid GetRequestId(HttpContext? context) => context?.Items[REQUEST_ID] as Guid? ?? Guid.NewGuid();

    /// <summary>
    /// Returns the User Id associated with the current HTTP Request being processed by the controller.
    /// </summary>
    public Guid GetUserId() => GetUserId(_accessor.HttpContext);

    /// <summary>
    /// Returns the User Id associated with the current HTTP Request being processed by the controller.
    /// </summary>
    public static Guid GetUserId(HttpContext? context)
    {
        string loggedUserId = context?.User?.FindFirstValue("id") ?? string.Empty;
        return Guid.TryParse(loggedUserId, out var userId) ? userId : throw new ValidationException("Invalid logged user id.");
    }

    /// <summary>
    /// Returns the User name associated with the current HTTP Request being processed by the controller.
    /// </summary>
    public string GetUserName() => GetUserName(_accessor.HttpContext);

    /// <summary>
    /// Returns the User name associated with the current HTTP Request being processed by the controller.
    /// </summary>
    public static string GetUserName(HttpContext? context) => context?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;


    /// <summary>
    /// Returns if the current HTTP Request being processed by the controller is authenticated.
    /// </summary>
    public bool IsAuthenticated() => IsAuthenticated(_accessor.HttpContext);

    /// <summary>
    /// Returns if the current HTTP Request being processed by the controller is authenticated.
    /// </summary>
    public static bool IsAuthenticated(HttpContext? context) => context?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Returns useful context information associated with the current HTTP Request being processed by the controller.
    /// </summary>
    /// <param name="logRequestBody">Indicates whether to fill <see cref="RequestContextInfo.RequestBody"/> .</param>
    public ValueTask<RequestContextInfo> GetRequestContextInfo(bool logRequestBody = true) =>
        GetRequestContextInfo(_accessor.HttpContext, logRequestBody);

    /// <summary>
    /// Returns useful context information associated with the current HTTP Request being processed by the controller.
    /// </summary>
    /// <param name="context">HttpContext of the request.</param>
    /// <param name="logRequestBody">Indicates whether to fill <see cref="RequestContextInfo.RequestBody"/> .</param>
    public static async ValueTask<RequestContextInfo> GetRequestContextInfo(HttpContext? context, bool logRequestBody = true)
    {
        if (context == null)
            return default!;

        var requestId = GetRequestId(context);
        string method = context.Request.Method;
        string path = context.Request.Path;
        string username = "Anonymous";
        string requestBody = string.Empty;
        string ip = context.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;
        var actionDescriptor = context.GetEndpoint()?.Metadata?.GetMetadata<ControllerActionDescriptor>();
        string controlerName = actionDescriptor?.ControllerTypeInfo?.FullName ?? string.Empty;

        if (IsAuthenticated(context))
            username = $"{GetUserName(context)} ({GetUserId(context)})";

        string contentType = context.Request.ContentType ?? string.Empty;

        if (logRequestBody &&
            context.Request.Body.CanRead &&
            context.Request.Body.Length < 32768 &&
            (method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Patch) &&
            contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 512, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
        }

        return new RequestContextInfo(requestId, method, path, controlerName, username, ip, requestBody);
    }
}

/// <summary>
/// Record class that contains useful context information related to a request being currently by a Controller.
/// </summary>
/// <param name="RequestId">Guid identifying the current request.</param>
/// <param name="Method">HTTP Verb used in the request.</param>
/// <param name="Path">URL Path of the request.</param>
/// <param name="ControllerName">Name of the controller class processing the request.</param>
/// <param name="Username">Name of the user logged in the request.</param>
/// <param name="IP">IP address of the request.</param>
/// <param name="RequestBody">Body content of the request when it's "application/json".</param>
public record RequestContextInfo(
    Guid RequestId,
    string Method,
    string Path,
    string ControllerName,
    string Username,
    string IP,
    string RequestBody);
