// Ignore Spelling: Mongo

using DocManager.Interfaces;

using MongoDB.Driver;

namespace DocManager.Data;

/// <summary>
/// Implements seeding for an empty MongoDB database.
/// </summary>
public class MongoDBSeeder
{
    /// <summary>
    /// Executes the database seeding.
    /// </summary>
    public static void Seed(IServiceScope scope)
    {
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<IMongoDatabase>();
        var logger = services.GetRequiredService<ILogger<MongoDBSeeder>>();

        var opt = new CreateCollectionOptions { Collation = new Collation("en", strength: CollationStrength.Secondary) };

        var collections = db.ListCollectionNames().ToEnumerable().ToHashSet(StringComparer.OrdinalIgnoreCase);
        logger.LogInformation("Seed: Fetched collection list.");
        if (!collections.Contains("users"))
        {
            logger.LogInformation("Seed: Collection USERS not found.");
            db.CreateCollection("users", opt);
            logger.LogInformation("Seed: Created USERS collection.");

            var repo = services.GetRequiredService<IUserRepository>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            repo.SeedUsers(hasher, logger);
        }

        if (!collections.Contains("documents"))
        {
            logger.LogInformation("Seed: Collection DOCUMENTS not found.");
            db.CreateCollection("documents", opt);
            logger.LogInformation("Seed: Created DOCUMENTS collection.");

            var repo = services.GetRequiredService<IDocumentRepository>();
            repo.SeedDocuments(logger);

        }
    }
}
