// Ignore Spelling: Mongo

using DocManager.Interfaces;
using DocManager.Models;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace DocManager.Repositories;

/// <inheritdoc cref="IDocumentRepository"/>
/// <param name="db">Injected instance of Mongo database object.</param>
/// <remarks>Uses MongoDB</remarks>
public class MongoDocumentRepository(IMongoDatabase db) : IDocumentRepository
{
    /// <summary>
    /// Collection name
    /// </summary>
    public const string COLLECTION = "documents";

    private readonly IMongoDatabase _db = db;

    private class DocumentMongoDao : DocumentModel
    {
        public ObjectId Id { get; set; }
        public ObjectId GridFSId { get; set; }
    }

    /// <inheritdoc/>
    public async ValueTask<DocumentModel> CreateDocument(DocumentModel document, Stream content, CancellationToken ct)
    {
        var docColl = _db.GetCollection<DocumentMongoDao>(COLLECTION);
        var filter = Builders<DocumentMongoDao>.Filter.Eq(d => d.DocumentId, document.DocumentId);

        if (await docColl.Find(filter).AnyAsync(ct))
            throw new Exception("Document Id already exists.");

        var bucket = GetDocumentBucket();
        var fileId = await bucket.UploadFromStreamAsync(document.Filename, content, cancellationToken: ct);

        var newDocument = new DocumentMongoDao
        {
            DocumentId = document.DocumentId,
            Filename = document.Filename,
            FileSize = document.FileSize,
            MimeType = document.MimeType,
            Tags = document.Tags,
            CreatedByUserId = document.CreatedByUserId,
            CreateTS = document.CreateTS,
            UpdatedByUserId = null,
            UpdateTS = null,
            GridFSId = fileId
        };

        await docColl.InsertOneAsync(newDocument, null, ct);

        return newDocument;
    }

    /// <inheritdoc/>
    public async ValueTask<DocumentModel> GetDocument(Guid id, CancellationToken ct)
    {
        var docColl = _db.GetCollection<DocumentMongoDao>(COLLECTION);
        var filter = Builders<DocumentMongoDao>.Filter.Eq(d => d.DocumentId, id);
        var doc = await docColl.Find(filter).FirstOrDefaultAsync(ct);

        return doc;
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<DocumentModel>> SearchDocument(string? name, string? tag, CancellationToken ct)
    {
        var docColl = _db.GetCollection<DocumentMongoDao>(COLLECTION);

        List<DocumentMongoDao>? docs;

        FilterDefinition<DocumentMongoDao>? filter = null, nameFilter = null, tagFilter = null;

        if (!string.IsNullOrWhiteSpace(name))
            nameFilter = Builders<DocumentMongoDao>.Filter.Text(name.Trim(), new TextSearchOptions { CaseSensitive = false });

        if (!string.IsNullOrWhiteSpace(tag))
            tagFilter = Builders<DocumentMongoDao>.Filter.AnyEq(d => d.Tags, tag.Trim());

        if (nameFilter is null && tagFilter is null)
        {
            docs = await docColl.AsQueryable().ToListAsync(ct);
        }
        else
        {
            filter = nameFilter is not null && tagFilter is not null
                ? nameFilter & tagFilter
                : nameFilter is not null
                    ? nameFilter
                    : tagFilter;
            docs = await docColl.Find(filter).ToListAsync(ct);
        }

        return docs;
    }

    /// <inheritdoc/>
    public async ValueTask<(string, Stream)> DownloadDocument(Guid id, CancellationToken ct)
    {
        var docColl = _db.GetCollection<DocumentMongoDao>(COLLECTION);
        var filter = Builders<DocumentMongoDao>.Filter.Eq(d => d.DocumentId, id);
        var projection = Builders<DocumentMongoDao>.Projection
            .Include(d => d.Filename)
            .Include(d => d.GridFSId);

        var doc = await docColl.Find(filter).Project(projection).FirstOrDefaultAsync(ct) ?? throw new Exception("Document not found.");

        var bucket = GetDocumentBucket();
        var stream = await bucket.OpenDownloadStreamAsync(doc["gridFSId"].AsObjectId, new GridFSDownloadOptions { Seekable = true }, ct);

        return (doc["filename"].AsString, stream);
    }

    /// <inheritdoc/>
    public async ValueTask<DocumentModel> UpdateTags(Guid id, ISet<string> tags, Guid updater, DateTimeOffset updateTS, CancellationToken ct)
    {
        var docColl = _db.GetCollection<DocumentMongoDao>(COLLECTION);
        var filter = Builders<DocumentMongoDao>.Filter.Eq(d => d.DocumentId, id);
        var update = Builders<DocumentMongoDao>.Update
            .Set(d => d.Tags, tags)
            .Set(d => d.UpdatedByUserId, updater)
            .Set(d => d.UpdateTS, updateTS);

        var result = await docColl.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false }, ct);
        if (!result.IsAcknowledged)
            throw new Exception("No record was inserted or updated.");

        var doc = await docColl.Find(filter).FirstOrDefaultAsync(ct);
        return doc;
    }

    /// <inheritdoc/>
    public void SeedDocuments(ILogger logger)
    {
        var docColl = _db.GetCollection<DocumentMongoDao>("documents");
        var uniqueOptions = new CreateIndexOptions { Unique = true };
        var key = Builders<DocumentMongoDao>.IndexKeys.Ascending(d => d.DocumentId);
        var indexModel = new CreateIndexModel<DocumentMongoDao>(key, uniqueOptions);
        docColl.Indexes.CreateOne(indexModel);
        logger.LogInformation("Seed: Created DOCUMENT ID index.");

        key = Builders<DocumentMongoDao>.IndexKeys.Text(d => d.Filename);
        var textOptions = new CreateIndexOptions { Collation = new Collation("simple", strength: CollationStrength.Secondary) };
        indexModel = new CreateIndexModel<DocumentMongoDao>(key, textOptions);
        docColl.Indexes.CreateOne(indexModel);
        logger.LogInformation("Seed: Created FILENAME TEXT index.");

        key = Builders<DocumentMongoDao>.IndexKeys.Ascending(d => d.Tags);
        indexModel = new CreateIndexModel<DocumentMongoDao>(key);
        docColl.Indexes.CreateOne(indexModel);
        logger.LogInformation("Seed: Created TAGS index.");

        GetDocumentBucket();
    }

    private GridFSBucket GetDocumentBucket()
    {
        var bucket = new GridFSBucket(_db, new GridFSBucketOptions { BucketName = "documentData" });

        return bucket;
    }
}
