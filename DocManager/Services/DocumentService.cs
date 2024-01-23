using DocManager.DTOs;
using DocManager.Interfaces;
using DocManager.Models;

namespace DocManager.Services;

/// <inheritdoc cref="IDocumentService"/>
/// <param name="docRepo">Injected instance of document repository.</param>
/// <param name="userRepo">Injected instance of user repository.</param>
public class DocumentService(IDocumentRepository docRepo, IUserRepository userRepo) : IDocumentService
{
    private readonly IDocumentRepository _docRepo = docRepo;
    private readonly IUserRepository _userRepo = userRepo;

    /// <inheritdoc/>
    public async ValueTask<DocumentViewDTO> UploadDocument(string fileName, string mimeType, long fileSize, string? tags, Guid creator, Stream content, CancellationToken ct)
    {
        ISet<string> tagList = !string.IsNullOrEmpty(tags)
            ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet()
            : [];

        var docModel = new DocumentModel
        {
            DocumentId = Guid.NewGuid(),
            Filename = fileName,
            MimeType = mimeType,
            FileSize = fileSize,
            Tags = tagList,
            CreatedByUserId = creator,
            CreateTS = DateTimeOffset.UtcNow,
        };

        docModel = await _docRepo.CreateDocument(docModel, content, ct);
        var usernames = await GetAuditUsernames([docModel], ct);

        return ToDTO(docModel, usernames);
    }

    private async ValueTask<IDictionary<Guid, string>> GetAuditUsernames(IEnumerable<DocumentModel> docs, CancellationToken ct)
    {
        var ids = new HashSet<Guid>();

        foreach (var doc in docs)
        {
            ids.Add(doc.CreatedByUserId);

            if (doc.UpdatedByUserId is not null)
                ids.Add(doc.UpdatedByUserId.Value);
        }

        var usernames = await _userRepo.GetUserNames(ids, ct);
        return usernames;
    }

    private static DocumentViewDTO ToDTO(DocumentModel model, IDictionary<Guid, string>? usernames)
    {
        var dto = new DocumentViewDTO
        {
            DocumentId = model.DocumentId,
            Filename = model.Filename,
            MimeType = model.MimeType,
            FileSize = model.FileSize,
            Tags = model.Tags,
            CreateTS = model.CreateTS,
            UpdateTS = model.UpdateTS
        };

        if (usernames is not null)
        {
            if (usernames.TryGetValue(model.CreatedByUserId, out string? username))
                dto.CreatedBy = username;

            if (model.UpdatedByUserId is not null && usernames.TryGetValue(model.UpdatedByUserId.Value, out username))
                dto.UpdatedBy = username;
        }

        return dto;
    }

    /// <inheritdoc/>
    public async ValueTask<DocumentViewDTO?> GetDocument(Guid id, CancellationToken ct)
    {
        var doc = await _docRepo.GetDocument(id, ct);
        if (doc is not null)
        {
            var usernames = await GetAuditUsernames([doc], ct);
            return ToDTO(doc, usernames);
        }

        return null;
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<DocumentViewDTO>> SearchDocuments(string? name, string? tag, CancellationToken ct)
    {
        var docs = await _docRepo.SearchDocument(name, tag, ct);
        if (docs?.Count() > 0)
        {
            var usernames = await GetAuditUsernames(docs, ct);
            return docs.Select(d => ToDTO(d, usernames));
        }

        return Enumerable.Empty<DocumentViewDTO>();
    }

    /// <inheritdoc/>
    public async ValueTask<(string, Stream)> DownloadDocument(Guid id, CancellationToken ct)
    {
        var (filename, stream) = await _docRepo.DownloadDocument(id, ct);

        return (filename, stream);
    }

    /// <inheritdoc/>
    public async ValueTask<DocumentViewDTO> UpdateTags(Guid id, ISet<string> tags, Guid updater, CancellationToken ct)
    {
        var doc = await _docRepo.UpdateTags(id, tags, updater, DateTimeOffset.UtcNow, ct);
        var usernames = await GetAuditUsernames([doc], ct);

        return ToDTO(doc, usernames);
    }
}
