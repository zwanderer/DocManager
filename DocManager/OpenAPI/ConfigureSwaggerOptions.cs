using System.Reflection;

using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DocManager.OpenAPI;

/// <summary>
/// Class for setting up Swagger options.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>, IConfigureOptions<SwaggerUIOptions>
{
    /// <summary>
    /// Swagger Gen Configuration
    /// </summary>
    public void Configure(SwaggerGenOptions options)
    {
        options.OperationFilter<AuthorizeOperationFilter>();
        options.DescribeAllParametersInCamelCase();
        options.CustomSchemaIds(t => t.FullName);
        options.SwaggerDoc("v1", CreateOpenApiInfo());

        string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename), true);

        options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = "Authorization",
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });
    }

    private static OpenApiInfo CreateOpenApiInfo() => new()
    {
        Title = "DocManager.API",
        Version = "v1",
        Description = "Public API for Document Management"
    };

    /// <summary>
    /// Swagger UI Configuration
    /// </summary>
    public void Configure(SwaggerUIOptions options) => options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocManager.API v1");
}
