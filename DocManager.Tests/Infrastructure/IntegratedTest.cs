// Ignore Spelling: Json Admin

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using DocManager.DTOs;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;

namespace DocManager.Tests.Infrastructure;

[Trait("Category", "Integration")]
public abstract class IntegratedTest : IClassFixture<DocManagerApplication>
{
    protected readonly DocManagerApplication _application;
    protected readonly HttpClient _client;
    protected readonly IMongoDatabase _db;

    protected Guid CurrentUserId { get; private set; }
    public JsonSerializerOptions DefaultJsonOptions { get; }

    public IntegratedTest(DocManagerApplication application)
    {
        _application = application;
        _client = application.CreateClient();
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _db = application
            .Services
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IMongoDatabase>();

        DefaultJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        DefaultJsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    protected async ValueTask<HttpResponseMessage> LogAs(string? email, string? password)
    {
        var input = new AuthInputDTO
        {
            Email = email,
            Password = password
        };
        var resp = await _client.PostAsJsonAsync("api/Auth", input);
        return resp;
    }

    private async ValueTask FinishLogon(HttpResponseMessage message)
    {
        message.EnsureSuccessStatusCode();
        var output = await message.Content.ReadFromJsonAsync<AuthOutputDTO>(DefaultJsonOptions);

        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, output!.Token);

        CurrentUserId = output.UserId;
    }

    protected async ValueTask LogAsAdmin()
    {
        var resp = await LogAs("admin@email.com", "password");
        await FinishLogon(resp);
    }

    protected async ValueTask LogAsUser()
    {
        var resp = await LogAs("user@email.com", "password");
        await FinishLogon(resp);
    }
}
