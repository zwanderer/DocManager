// Ignore Spelling: Jwt Mongo app

using System.Security.Claims;

using System.Text;

using DocManager.Auth;
using DocManager.Data;
using DocManager.Interfaces;
using DocManager.Models;
using DocManager.OpenAPI;
using DocManager.Repositories;
using DocManager.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DocManager.Utils;

/// <summary>
/// Dependency Injection setup class
/// </summary>
public static class StartupSetup
{
    /// <summary>
    /// Configure Dependency Injection here.
    /// </summary>
    public static void InjectServices(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<IPasswordHasher, HMACPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSingleton<IConfigureOptions<SwaggerUIOptions>, ConfigureSwaggerOptions>();
    }

    /// <summary>
    /// Configure database settings here.
    /// </summary>
    public static void SetupDatabase(IServiceCollection services, IConfiguration config)
    {
        var pack = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("elementNameConvention", pack, x => true);

        string? connStr = config["ConnectionStrings:Database"];

        if (string.IsNullOrEmpty(connStr))
            services.AddSingleton<IMongoClient>(new MongoClient());
        else
            services.AddSingleton<IMongoClient>(new MongoClient(connStr));

        services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            string database = config["MongoDBName"]!;
            return client.GetDatabase(database);
        });

        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IDocumentRepository, MongoDocumentRepository>();
    }

    /// <summary>
    /// Execute DB seeding here.
    /// </summary>
    public static void SeedDatabase(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        MongoDBSeeder.Seed(scope);
    }

    /// <summary>
    /// Configure JWT authentication here.
    /// </summary>
    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(opt =>
        {
            byte[] key = Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!);
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(RoleType.Admin)
            .AddPolicy(RoleType.User);

        services.AddHttpContextAccessor();
    }

    private static AuthorizationBuilder AddPolicy(this AuthorizationBuilder builder, RoleType role) =>
        builder.AddPolicy(role.ToString(), p => p.RequireAuthenticatedUser().RequireRole(role.ToString()));
}