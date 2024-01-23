using System.Net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace DocManager.OpenAPI;

/// <summary>
/// Operation filter to indicate in Swagger if an endpoint will require Authentication.
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    private static readonly string[] BEARER = ["bearer"];

    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        bool authAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            ?.Any() ?? false;

        bool anonymousAttributes = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            ?.Any() ?? false;

        if (authAttributes && !anonymousAttributes)
        {
            operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse { Description = nameof(HttpStatusCode.Unauthorized) });
            operation.Responses.Add(StatusCodes.Status403Forbidden.ToString(), new OpenApiResponse { Description = nameof(HttpStatusCode.Forbidden) });

            operation.Security = new List<OpenApiSecurityRequirement>();

            var jwtSecurityScheme = new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" },
            };

            operation.Security.Add(new OpenApiSecurityRequirement()
            {
                [jwtSecurityScheme] = BEARER
            });
        }
    }
}

