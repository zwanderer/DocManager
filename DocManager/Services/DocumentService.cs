using DocManager.DTOs;
using DocManager.Exceptions;
using DocManager.Interfaces;
using DocManager.Models;
using DocManager.Utils;

namespace DocManager.Services;

/// <inheritdoc cref="IDocumentService"/>
/// <param name="docRepo">Injected instance of document repository.</param>
/// <param name="userRepo">Injected instance of user repository.</param>
/// <param name="avScanner">Injected AV Scanner service instance.</param>
/// <param name="logger">Injected logger service instance.</param>
/// <param name="requestContext">Injected Request Context instance.</param>
public class DocumentService(
    IDocumentRepository docRepo,
    IUserRepository userRepo,
    IAVScanner avScanner,
    ILogger<DocumentService> logger,
    RequestContext requestContext) : IDocumentService
{
    private readonly IDocumentRepository _docRepo = docRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IAVScanner _avScanner = avScanner;
    private readonly ILogger _logger = logger;
    private readonly RequestContext _requestContext = requestContext;

    /// <inheritdoc/>
    public async ValueTask<DocumentViewDTO> UploadDocument(string fileName, string mimeType, long fileSize, string? tags, Guid creator, Stream content, CancellationToken ct)
    {
        var requestId = _requestContext.GetRequestId();

        if (!ValidFileName(fileName))
            throw new ValidationException("Invalid file name.");

        if (fileSize <= 0)
            throw new ValidationException("Invalid file size.");

        (bool clean, string? virus) = await _avScanner.ScanFile(content, ct);

        if (!clean)
        {
            if (virus == "error")
            {
                _logger.LogInformation("[{requestId}] An error occurred while scanning the file `{filename}` for viruses.", requestId, fileName);
                throw new AntiVirusException("An error occurred while scanning the file for viruses.");
            }
            else
            {
                _logger.LogInformation("[{requestId}] Virus `{virus}` found in `{filename}`!!", requestId, virus, fileName);
                throw new AntiVirusException($"A virus named `{virus}` was detected in the uploaded document.");
            }
        }

        content.Seek(0, SeekOrigin.Begin);

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

        _logger.LogInformation("[{requestId}] Creating record for document `{documentId}` in storage.", requestId, docModel.DocumentId);
        docModel = await _docRepo.CreateDocument(docModel, content, ct);
        var usernames = await GetAuditUsernames([docModel], ct);

        return ToDTO(docModel, usernames);
    }

    private static readonly char[] INVALID_FILE_CHARS =
        Path.GetInvalidFileNameChars()
        .Union(Path.GetInvalidFileNameChars())
        .Union(['/', '\\'])
        .ToArray();

    private static bool ValidFileName(string fileName) => !string.IsNullOrEmpty(fileName) && !INVALID_FILE_CHARS.Any(fileName.Contains);

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
    public async ValueTask<(string?, Stream?)> DownloadDocument(Guid id, CancellationToken ct)
    {
        var (filename, stream) = await _docRepo.DownloadDocument(id, ct);

        return (filename, stream);
    }

    /// <inheritdoc/>
    public async ValueTask<DocumentViewDTO> UpdateTags(Guid id, ISet<string> tags, Guid updater, CancellationToken ct)
    {
        foreach (string tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag) || tag.Contains(','))
                throw new ValidationException($"`{tag}` is not a valid value for tag.");
        }

        var doc = await _docRepo.UpdateTags(id, tags, updater, DateTimeOffset.UtcNow, ct);
        var usernames = await GetAuditUsernames([doc], ct);

        return ToDTO(doc, usernames);
    }
}
