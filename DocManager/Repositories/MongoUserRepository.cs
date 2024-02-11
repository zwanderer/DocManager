// Ignore Spelling: Mongo

using DocManager.Exceptions;
using DocManager.Interfaces;
using DocManager.Models;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DocManager.Repositories;

/// <inheritdoc cref="IUserRepository"/>
/// <param name="db">Injected instance of Mongo database object.</param>
/// <remarks>Uses MongoDB</remarks>
public class MongoUserRepository(IMongoDatabase db) : IUserRepository
{
    /// <summary>
    /// Collection name
    /// </summary>
    public const string COLLECTION = "users";

    private readonly IMongoDatabase _db = db;

    private class UserMongoDao : UserModel
    {
        public ObjectId Id { get; set; }
    }

    /// <inheritdoc/>
    public async ValueTask<UserModel> GetUser(Guid id, CancellationToken ct)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var filter = Builders<UserMongoDao>.Filter.Eq(u => u.UserId, id);
        var user = await userColl.Find(filter).FirstOrDefaultAsync(ct);

        return user;
    }


    /// <inheritdoc/>
    public async ValueTask<UserModel> GetUserByEmail(string email, CancellationToken ct)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var filter = Builders<UserMongoDao>.Filter.Eq(u => u.Email, email.ToLower());
        var user = await userColl.Find(filter).FirstOrDefaultAsync(ct);

        return user;
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<UserModel>> GetUsers(CancellationToken ct)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var users = await userColl
            .AsQueryable()
            .ToListAsync(cancellationToken: ct);

        return users;
    }

    /// <inheritdoc/>
    public async ValueTask<IDictionary<Guid, string>> GetUserNames(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var filter = Builders<UserMongoDao>.Filter.In(u => u.UserId, ids);
        var projection = Builders<UserMongoDao>.Projection
            .Include(u => u.UserId)
            .Include(u => u.Email);
        var usernames = await userColl.Find(filter).Project(projection).ToListAsync(ct);

        var dictionary = new Dictionary<Guid, string>();
        foreach (var username in usernames)
            dictionary.Add(username["userId"].AsGuid, username["email"].AsString);

        return dictionary;
    }


    /// <inheritdoc/>
    public async ValueTask<UserModel> CreateOrUpdateUser(UserModel user, CancellationToken ct)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var filter = Builders<UserMongoDao>.Filter.Eq(u => u.UserId, user.UserId);
        var update = Builders<UserMongoDao>.Update
            .Set(u => u.Name, user.Name)
            .Set(u => u.PasswordHash, user.PasswordHash)
            .Set(u => u.RolesAssigned, user.RolesAssigned)
            .Set(u => u.UpdatedByUserID, user.UpdatedByUserID)
            .Set(u => u.UpdateTS, user.UpdateTS)
            .SetOnInsert(u => u.UserId, user.UserId)
            .SetOnInsert(u => u.Email, user.Email)
            .SetOnInsert(u => u.CreatedByUserId, user.CreatedByUserId)
            .SetOnInsert(u => u.CreateTS, user.CreateTS);

        var result = await userColl.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
        if (!result.IsAcknowledged)
            throw new StorageException("No record was inserted or updated.");

        user = await userColl.Find(filter).FirstOrDefaultAsync(ct);
        return user;
    }

    /// <inheritdoc/>
    public async ValueTask DeleteUser(Guid id, CancellationToken ct)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var filter = Builders<UserMongoDao>.Filter.Eq(u => u.UserId, id);
        var result = await userColl.DeleteOneAsync(filter, ct);

        if (!result.IsAcknowledged)
            throw new StorageException("No record was deleted.");
    }

    /// <inheritdoc/>
    public void SeedUsers(IPasswordHasher hasher, ILogger logger)
    {
        var userColl = _db.GetCollection<UserMongoDao>(COLLECTION);
        var options = new CreateIndexOptions { Unique = true };

        var key = Builders<UserMongoDao>.IndexKeys.Ascending(u => u.UserId);
        var indexModel = new CreateIndexModel<UserMongoDao>(key, options);
        userColl.Indexes.CreateOne(indexModel);
        logger.LogInformation("Seed: Created USER ID index.");

        key = Builders<UserMongoDao>.IndexKeys.Ascending(u => u.Email);
        indexModel = new CreateIndexModel<UserMongoDao>(key, options);
        userColl.Indexes.CreateOne(indexModel);
        logger.LogInformation("Seed: Created EMAIL index.");

        var adminUserId = Guid.NewGuid();
        const string adminEmail = "admin@email.com";
        const string defaultPassword = "password";
        string adminHash = hasher.HashPassword(adminEmail, defaultPassword);

        var adminUser = new UserMongoDao
        {
            UserId = adminUserId,
            Name = "Administrator",
            Email = adminEmail,
            RolesAssigned = new HashSet<RoleType> { RoleType.Admin, RoleType.User },
            CreatedByUserId = adminUserId,
            CreateTS = DateTimeOffset.UtcNow,
            PasswordHash = adminHash
        };

        var regularUserId = Guid.NewGuid();
        const string regularEmail = "user@email.com";
        string regularHash = hasher.HashPassword(regularEmail, defaultPassword);
        var regularUser = new UserMongoDao
        {
            UserId = regularUserId,
            Name = "Regular User",
            Email = regularEmail,
            RolesAssigned = new HashSet<RoleType> { RoleType.User },
            CreatedByUserId = adminUserId,
            CreateTS = DateTimeOffset.UtcNow,
            PasswordHash = regularHash
        };

        userColl.InsertMany([adminUser, regularUser]);
        logger.LogInformation("Seed: Created ADMIN and REGULAR users.");
    }
}
