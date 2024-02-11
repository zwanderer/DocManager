// Ignore Spelling: Admin

using System.Net;
using System.Net.Http.Json;

using DocManager.DTOs;
using DocManager.Models;
using DocManager.Repositories;
using DocManager.Tests.Infrastructure;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using MongoDB.Bson;
using MongoDB.Driver;

using Xunit.Abstractions;

namespace DocManager.Tests.Tests;

public class UserTests(DocManagerApplication application, ITestOutputHelper output) : IntegratedTest(application, output)
{
    [Fact]
    public async Task CanGetAllUsers_IfAdmin()
    {
        await LogAsAdmin();
        var resp = await _client.GetAsync("api/user");
        var usersFromApi = await resp.Content.ReadFromJsonAsync<List<UserViewDTO>>(DefaultJsonOptions);

        var userColl = _db.GetCollection<BsonDocument>(MongoUserRepository.COLLECTION);
        var usersFromDB = await userColl.AsQueryable().ToListAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        usersFromApi.Should().NotBeNullOrEmpty();
        usersFromDB.Should().NotBeNullOrEmpty();
        usersFromApi.Should().HaveCount(usersFromDB.Count);
    }

    [Fact]
    public async Task CanGetOneUser_IfAdmin()
    {
        await LogAsAdmin();
        var resp = await _client.GetAsync($"api/user/{CurrentUserId}");
        var userFromApi = await resp.Content.ReadFromJsonAsync<UserViewDTO>(DefaultJsonOptions);

        var userColl = _db.GetCollection<BsonDocument>(MongoUserRepository.COLLECTION);
        var filter = Builders<BsonDocument>.Filter.Eq(u => u["userId"].AsGuid, CurrentUserId);
        var userFromDB = await userColl.Find(filter).FirstOrDefaultAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        userFromApi.Should().NotBeNull();
        userFromDB.Should().NotBeNull();
        userFromApi!.Name.Should().Be(userFromDB["name"].AsString);
        userFromApi.Email.Should().Be(userFromDB["email"].AsString);
        userFromApi.RolesAssigned.Should().Contain(RoleType.Admin);
        userFromApi.RolesAssigned.Should().Contain(RoleType.User);
    }

    [Fact]
    public async Task CannotGetAllUser_IfUser()
    {
        await LogAsUser();
        var resp = await _client.GetAsync("api/user");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [Repeat(5)]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public async Task NoContent_IfInvalidUserId(int step)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        await LogAsAdmin();
        var userId = Guid.NewGuid();
        var resp = await _client.GetAsync($"api/user/{userId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("", "em@em.com", "a")]
    [InlineData("David", "a@a", "asdfgghjasd")]
    [InlineData(null, null, null)]
    [InlineData("", "test@test.com", "123123123")]
    [InlineData("who?", "test@test.com", "123")]
    public async Task CannotCreateUser_IfInvalidInput(string? name, string? email, string? password)
    {
        await LogAsAdmin();
        var dto = new CreateUserDTO
        {
            Name = name!,
            Email = email!,
            Password = password!,
            RolesAssigned = new HashSet<RoleType> { RoleType.Admin }
        };
        const string expectedError = "One or more validation errors occurred.";

        var resp = await _client.PostAsJsonAsync($"api/user", dto, DefaultJsonOptions);
        var error = await resp.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().NotBeNull();
        error!.Title.Should().Be(expectedError);
        error.Errors.Should().NotBeNull();
        error.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Someone", "someone@email.com", "Password@123")]
    [InlineData("Who", "who@email.com", "Password@123")]
    [InlineData("Cares", "cares@email.com", "Password@123")]
    public async Task CanCreateUser_IfAdminAndValidInput(string name, string email, string password)
    {
        await LogAsAdmin();
        var dto = new CreateUserDTO
        {
            Name = name!,
            Email = email!,
            Password = password!,
            RolesAssigned = new HashSet<RoleType> { RoleType.User }
        };

        var resp = await _client.PostAsJsonAsync($"api/user", dto, DefaultJsonOptions);
        var newUser = await resp.Content.ReadFromJsonAsync<UserViewDTO>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        newUser.Should().NotBeNull();
        newUser!.Name.Should().Be(name);
        newUser.Email.Should().Be(email);
        newUser.CreatedBy.Should().Be("admin@email.com");
        newUser.RolesAssigned.Should().Contain(RoleType.User);

        resp = await LogAs(email, password);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Someone", "someone2@email.com", "Password@123", "", "")]
    [InlineData("Who", "who2@email.com", "Password@123", "asd", "asd")]
    [InlineData("Cares", "cares2@email.com", "Password@123", "", "123123123")]
    public async Task CannotUpdateUser_IfInvalidInput(string name, string email, string password, string invalidName, string invalidPassword)
    {
        await LogAsAdmin();
        var create = new CreateUserDTO
        {
            Name = name,
            Email = email,
            Password = password,
            RolesAssigned = new HashSet<RoleType> { RoleType.Admin }
        };
        const string expectedError = "One or more validation errors occurred.";

        var resp = await _client.PostAsJsonAsync($"api/user", create, DefaultJsonOptions);
        var newUser = await resp.Content.ReadFromJsonAsync<UserViewDTO>(DefaultJsonOptions);

        var id = newUser!.UserId;

        var update = new UpdateUserDTO
        {
            Name = invalidName,
            Password = invalidPassword,
            RolesAssigned = null
        };

        resp = await _client.PatchAsJsonAsync($"api/user/{id}", update, DefaultJsonOptions);
        var error = await resp.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().NotBeNull();
        error!.Title.Should().Be(expectedError);
        error.Errors.Should().NotBeNull();
        error.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Someone", "someone3@email.com", "Password@123", "NewName", "NewPassword")]
    [InlineData("Who", "who3@email.com", "Password@123", "AnotherName", "AnotherPassword")]
    [InlineData("Cares", "cares3@email.com", "Password@123", "Maybe", "MaybeNot")]
    public async Task CanUpdateUser_IfAdminAndValidInput(string name, string email, string password, string newName, string newPassword)
    {
        await LogAsAdmin();
        var dto = new CreateUserDTO
        {
            Name = name,
            Email = email!,
            Password = password!,
            RolesAssigned = new HashSet<RoleType> { RoleType.User }
        };

        var resp = await _client.PostAsJsonAsync($"api/user", dto, DefaultJsonOptions);
        var newUser = await resp.Content.ReadFromJsonAsync<UserViewDTO>(DefaultJsonOptions);

        var id = newUser!.UserId;

        var update = new UpdateUserDTO
        {
            Name = newName,
            Password = newPassword,
            RolesAssigned = null
        };

        resp = await _client.PatchAsJsonAsync($"api/user/{id}", update, DefaultJsonOptions);
        newUser = await resp.Content.ReadFromJsonAsync<UserViewDTO>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        newUser.Should().NotBeNull();
        newUser!.Name.Should().Be(newName);
        newUser.Email.Should().Be(email);
        newUser.CreatedBy.Should().Be("admin@email.com");
        newUser.RolesAssigned.Should().Contain(RoleType.User);

        resp = await LogAs(email, newPassword);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Someone Else", "someoneelse@email.com", "Password@123")]
    [InlineData("Who Doesn't", "whodoesnt@email.com", "Password@123")]
    [InlineData("Cares At All", "caresatall@email.com", "Password@123")]
    public async Task CanDeleteUser_IfAdmin(string name, string email, string password)
    {
        await LogAsAdmin();
        var dto = new CreateUserDTO
        {
            Name = name,
            Email = email!,
            Password = password!,
            RolesAssigned = new HashSet<RoleType> { RoleType.User }
        };

        var resp = await _client.PostAsJsonAsync($"api/user", dto, DefaultJsonOptions);
        var newUser = await resp.Content.ReadFromJsonAsync<UserViewDTO>(DefaultJsonOptions);

        var id = newUser!.UserId;

        resp = await _client.DeleteAsync($"api/user/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        resp = await LogAs(email, password);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CannotDeleteItself()
    {
        await LogAsAdmin();
        var resp = await _client.DeleteAsync($"api/user/{CurrentUserId}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
