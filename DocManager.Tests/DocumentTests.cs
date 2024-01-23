using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using DocManager.DTOs;
using DocManager.Tests.Infrastructure;

using FluentAssertions;

namespace DocManager.Tests;

public class DocumentTests(DocManagerApplication application) : IntegratedTest(application)
{
    private static HttpRequestMessage GenerateUploadRequest(string fileName, string mimeType, string content, string tags)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/document")
        {
            Content = new MultipartFormDataContent
            {
                { new StringContent(content, new MediaTypeHeaderValue(mimeType)), "file", fileName }
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
}
