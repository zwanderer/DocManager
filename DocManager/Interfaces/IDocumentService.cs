using DocManager.DTOs;
using DocManager.Models;

namespace DocManager.Interfaces;

/// <summary>
/// Provides handling of business logic for <see cref="DocumentModel"/>.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Uploads a new document.
    /// </summary>
    /// <param name="fileName">File name of the document.</param>
    /// <param name="mimeType">Mime type of the document.</param>
    /// <param name="fileSize">Size in bytes of the document.</param>
    /// <param name="tags">List of comma separated tags to be associated with the document.</param>
    /// <param name="creator">Id of the user who uploaded the document.</param>
    /// <param name="content">Stream with the document binary content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentViewDTO"/> with the document details.</returns>
    ValueTask<DocumentViewDTO> UploadDocument(string fileName, string mimeType, long fileSize, string? tags, Guid creator, Stream content, CancellationToken ct);

    /// <summary>
    /// Returns the details of a document.
    /// </summary>
    /// <param name="id">Id of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentViewDTO"/> with the document details.</returns>
    ValueTask<DocumentViewDTO?> GetDocument(Guid id, CancellationToken ct);

    /// <summary>
    /// Searches documents by name or tag.
    /// </summary>
    /// <param name="name">Document name to look for.</param>
    /// <param name="tag">Document tag to look for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="DocumentViewDTO"/> instances of the documents that match the criteria.</returns>
    ValueTask<IEnumerable<DocumentViewDTO>> SearchDocuments(string? name, string? tag, CancellationToken ct);

    /// <summary>
    /// Downloads the content of a document.
    /// </summary>
    /// <param name="id">Id of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple with file name and stream with the binary content of the document.</returns>
    ValueTask<(string, Stream)> DownloadDocument(Guid id, CancellationToken ct);

    /// <summary>
    /// Updates the tags of a document.
    /// </summary>
    /// <param name="id">Id of the document.</param>
    /// <param name="tags">List of tags.</param>
    /// <param name="updater">Id of user who updated the tags.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentViewDTO"/> with the document details.</returns>
    ValueTask<DocumentViewDTO> UpdateTags(Guid id, ISet<string> tags, Guid updater, CancellationToken ct);
}
