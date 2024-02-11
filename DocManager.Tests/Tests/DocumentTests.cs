// Ignore Spelling: Admin

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using DocManager.DTOs;
using DocManager.Exceptions;
using DocManager.Tests.Infrastructure;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Xunit.Abstractions;

namespace DocManager.Tests.Tests;

public class DocumentTests(DocManagerApplication application, ITestOutputHelper output) : IntegratedTest(application, output)
{
    private static HttpRequestMessage GenerateUploadRequest(string fileName, string mimeType, string content, string tags)
    {
        var formContent = new MultipartFormDataContent();
        var stringContent = new StringContent(content, new MediaTypeHeaderValue(mimeType));
        if (string.IsNullOrWhiteSpace(fileName))
            formContent.Add(stringContent, "file");
        else
            formContent.Add(stringContent, "file", fileName);

        var req = new HttpRequestMessage(HttpMethod.Post, "api/document") { Content = formContent };

        req.Headers.Add("x-tags", tags);

        return req;
    }

    private static async ValueTask<HttpRequestMessage> GenerateUploadRequest(string resourceName, string tags)
    {
        var assembly = typeof(DocumentTests).Assembly;
        resourceName = resourceName.Replace("/", ".");
        var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new ArgumentNullException(nameof(resourceName));

        if (resourceName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
        {
            var ms = new MemoryStream();
            await FileCryptLib.FileCrypt.Decrypt(stream, ms);
            stream = ms;
        }

        var req = new HttpRequestMessage(HttpMethod.Post, "api/document")
        {
            Content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", resourceName }
            }
        };

        req.Headers.Add("x-tags", tags);

        return req;
    }

    [Theory]
    [InlineData("file.txt", "text/plain", "test", "tag1,tag2")]
    [InlineData("file2.jpg", "image/jpg", "test123", "")]
    [InlineData("file3.pdf", "application/pdf", "test456", "pdf,contract,sales,something")]
    public async Task CanUploadDocument_AsUser(string fileName, string mimeType, string content, string tags)
    {
        await LogAsUser();
        var request = GenerateUploadRequest(fileName, mimeType, content, tags);
        var resp = await _client.SendAsync(request);
        var doc = await resp.Content.ReadFromJsonAsync<DocumentViewDTO>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        doc.Should().NotBeNull();
        doc!.Filename.Should().Be(fileName);
        doc.FileSize.Should().Be(content.Length);
        doc.MimeType.Should().Be(mimeType);
        doc.CreatedBy.Should().Be("user@email.com");

        if (!string.IsNullOrEmpty(tags))
        {
            var splitTags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

            foreach (string tag in splitTags)
                doc.Tags.Should().Contain(tag);

            foreach (string tag in doc.Tags)
                splitTags.Should().Contain(tag);
        }
    }

    [Theory]
    [InlineData("file4.txt", "text/plain", "test123321", "tag4,tag6")]
    [InlineData("file5.txt", "text/plain", "abcdefghiKja", "tag5,tag7")]
    public async Task CanDownloadDocument_AsUser(string fileName, string mimeType, string content, string tags)
    {
        await LogAsUser();
        var request = GenerateUploadRequest(fileName, mimeType, content, tags);
        var resp = await _client.SendAsync(request);
        var doc = await resp.Content.ReadFromJsonAsync<DocumentViewDTO>(DefaultJsonOptions);
        var id = doc!.DocumentId;
        resp = await _client.GetAsync($"api/document/download/{id}");
        string download = await resp.Content.ReadAsStringAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Should().NotBeNullOrEmpty();
        download.Should().Be(content);
    }

    [Theory]
    [InlineData("file4.txt", "text/plain", "test123321", "tag4,tag6")]
    [InlineData("file5.txt", "text/plain", "abcdefghiKja", "tag5,tag7")]
    public async Task CanDownloadDocument_AsAdmin(string fileName, string mimeType, string content, string tags)
    {
        await LogAsAdmin();
        var request = GenerateUploadRequest(fileName, mimeType, content, tags);
        var resp = await _client.SendAsync(request);
        var doc = await resp.Content.ReadFromJsonAsync<DocumentViewDTO>(DefaultJsonOptions);
        var id = doc!.DocumentId;
        resp = await _client.GetAsync($"api/document/download/{id}");
        string download = await resp.Content.ReadAsStringAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Should().NotBeNullOrEmpty();
        download.Should().Be(content);
    }

    [Fact]
    public async Task CannotDownloadDocument_IfNotLogged()
    {
        LogOff();
        var resp = await _client.GetAsync($"api/document/download/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("file.txt", "text/plain", "test", "tag1,tag2")]
    [InlineData("file2.jpg", "image/jpg", "test123", "blablabla")]
    [InlineData("file3.pdf", "application/pdf", "test456", "pdf,contract,sales,something")]
    public async Task CanSearchDocuments_ByTag(string fileName, string mimeType, string content, string tags)
    {
        await LogAsUser();
        var request = GenerateUploadRequest(fileName, mimeType, content, tags);
        var resp = await _client.SendAsync(request);
        var doc = await resp.Content.ReadFromJsonAsync<DocumentViewDTO>(DefaultJsonOptions);
        var id = doc!.DocumentId;

        foreach (string tag in doc.Tags)
        {
            resp = await _client.GetAsync($"api/document?tag={tag}");
            var documents = await resp.Content.ReadFromJsonAsync<List<DocumentViewDTO>>(DefaultJsonOptions);

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            documents.Should().NotBeNullOrEmpty();
            documents.Should().Contain(d => d.DocumentId == id);
        }
    }

    [Theory]
    [InlineData("file.txt", "text/plain", "test", "tag1,tag2", "blablabla")]
    [InlineData("file2.jpg", "image/jpg", "test123", "blablabla", "pdf,contract,sales,something")]
    [InlineData("file3.pdf", "application/pdf", "test456", "pdf,contract,sales,something", "tag1,tag2")]
    public async Task CanUpdateTags(string fileName, string mimeType, string content, string tags, string newTags)
    {
        await LogAsUser();
        var request = GenerateUploadRequest(fileName, mimeType, content, tags);
        var resp = await _client.SendAsync(request);
        var doc = await resp.Content.ReadFromJsonAsync<DocumentViewDTO>(DefaultJsonOptions);
        var id = doc!.DocumentId;
        var splitTags = newTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        resp = await _client.PatchAsJsonAsync($"api/document/{id}", splitTags, DefaultJsonOptions);
        doc = await resp.Content.ReadFromJsonAsync<DocumentViewDTO>(DefaultJsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (string tag in splitTags)
            doc!.Tags.Should().Contain(tag);

        foreach (string tag in doc!.Tags)
            splitTags.Should().Contain(tag);
    }

    [Theory]
    [InlineData("file2.jpg", "image/jpg", "", "")]
    [InlineData("", "application/pdf", "test456", "pdf,contract,sales,something")]
    [InlineData("asd/dsa.@", "text/plain", "test456", "")]
    [InlineData("????", "text/plain", "test456", "")]
    [InlineData("aaa\\aaa\a", "text/plain", "test456", "")]
    public async Task CannotUpload_IfInputInvalid(string fileName, string mimeType, string content, string tags)
    {
        await LogAsUser();
        var request = GenerateUploadRequest(fileName, mimeType, content, tags);
        var resp = await _client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("DocManager.Tests/Resources/eicarcom2.zip.enc", "virus")]
    public async Task CannotUploadVirus(string resourceName, string tags)
    {
        await LogAsUser();
        var request = await GenerateUploadRequest(resourceName, tags);
        var resp = await _client.SendAsync(request);
        var details = await resp.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        details.Should().NotBeNull();
        details!.Detail.Should().Be(nameof(AntiVirusException));
        details.Errors.Should().NotBeNullOrEmpty();
        details.Errors.Should().ContainKey("ErrorMessage");
        details.Errors["ErrorMessage"].Should().Contain(x => x.Contains("Win.Test.EICAR_HDB-1", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [Repeat(5)]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public async Task NoContent_IfInvalidDocumentId(int step)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        await LogAsUser();
        var documentId = Guid.NewGuid();
        var resp = await _client.GetAsync($"api/document/{documentId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

}
