using System.Net;
using System.Net.Http.Json;

using DocManager.Controllers;
using DocManager.DTOs;
using DocManager.Tests.Infrastructure;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace DocManager.Tests;

public class AuthenticationTests(DocManagerApplication application) : IntegratedTest(application)
{
    [Theory]
    [InlineData("invalid@email.com", "invalidPassword")]
    [InlineData("wat@email.com", "password")]
    [InlineData("admin@email.com", "nopeagain")]
    public async Task Unauthorized_IfInvalidCredentials(string email, string password)
    {
        var resp = await LogAs(email, password);
        const string expectedError = AuthController.AUTH_ERROR_MESSAGE;
        string message = await resp.Content.ReadFromJsonAsync<string>(DefaultJsonOptions) ?? string.Empty;

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        message.Should().Be(expectedError);
    }

    [Theory]
    [InlineData("invalidemail.com", "123")]
    [InlineData("", null)]
    [InlineData("admin@email.com", "nope")]
    public async Task BadRequest_IfInvalidInput(string? email, string? password)
    {
        var resp = await LogAs(email, password);
        const string expectedError = "One or more validation errors occurred.";
        var error = await resp.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().NotBeNull();
        error!.Title.Should().Be(expectedError);
        error.Errors.Should().NotBeNull();
        error.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("admin@email.com", "password")]
    [InlineData("user@email.com", "password")]
    public async Task TokenReturned_IfValidCredentials(string email, string password)
    {
        var resp = await LogAs(email, password);
        var output = await resp.Content.ReadFromJsonAsync<AuthOutputDTO>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        output.Should().NotBeNull();
        output!.Token.Should().NotBeNull();
        output.Token.Should().NotBeEmpty();
    }
}
