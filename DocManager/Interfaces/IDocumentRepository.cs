using DocManager.Models;

namespace DocManager.Interfaces;

/// <summary>
/// Repository for storing <see cref="DocumentModel"/>.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Creates a document entity in the storage.
    /// </summary>
    /// <param name="document">Document metadata.</param>
    /// <param name="content">Document binary content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentModel"/> with the stored document metadata.</returns>
    ValueTask<DocumentModel> CreateDocument(DocumentModel document, Stream content, CancellationToken ct);

    /// <summary>
    /// Returns the metadata of a document from storage.
    /// </summary>
    /// <param name="id">Id of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentModel"/> with the stored document metadata.</returns>
    ValueTask<DocumentModel> GetDocument(Guid id, CancellationToken ct);

    /// <summary>
    /// Searches for document in storage based on name or tag.
    /// </summary>
    /// <param name="name">Name of document to look for.</param>
    /// <param name="tag">Tag of the document to look for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="DocumentModel"/> with found documents.</returns>
    ValueTask<IEnumerable<DocumentModel>> SearchDocument(string? name, string? tag, CancellationToken ct);

    /// <summary>
    /// Returns the binary content of a document.
    /// </summary>
    /// <param name="id">Id of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple with file name and stream with the document's binary content.</returns>
    ValueTask<(string, Stream)> DownloadDocument(Guid id, CancellationToken ct);

    /// <summary>
    /// Updates the list of tags of a document.
    /// </summary>
    /// <param name="id">Id of the document.</param>
    /// <param name="tags">List of tags.</param>
    /// <param name="updater">Id of user who updated the tags.</param>
    /// <param name="updateTS">Timestamp of when the tags were updated.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="DocumentModel"/> with the stored document metadata.</returns>
    ValueTask<DocumentModel> UpdateTags(Guid id, ISet<string> tags, Guid updater, DateTimeOffset updateTS, CancellationToken ct);

    /// <summary>
    /// Seeds the database with the initial documents.
    /// </summary>
    /// <param name="logger">Instance of logger.</param>
    void SeedDocuments(ILogger logger);
}
