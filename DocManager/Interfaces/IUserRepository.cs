using DocManager.Models;

namespace DocManager.Interfaces;

/// <summary>
/// Repository for storing <see cref="UserModel"/>.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Returns a single user.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserModel"/>.</returns>
    ValueTask<UserModel> GetUser(Guid id, CancellationToken ct);

    /// <summary>
    /// Returns a single user based on email.
    /// </summary>
    /// <param name="email">Email of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserModel"/>.</returns>
    ValueTask<UserModel> GetUserByEmail(string email, CancellationToken ct);

    /// <summary>
    /// Returns a list of all users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list containing instances of <see cref="UserModel"/>.</returns>
    ValueTask<IEnumerable<UserModel>> GetUsers(CancellationToken ct);

    /// <summary>
    /// Returns a list of usernames based on a list of ids.
    /// </summary>
    /// <param name="ids">List of user ids.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary containing Ids and usernames.</returns>
    ValueTask<IDictionary<Guid, string>> GetUserNames(IEnumerable<Guid> ids, CancellationToken ct);

    /// <summary>
    /// Creates or Updates an user.
    /// </summary>
    /// <param name="user">User data to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserModel"/> representing the newly created or updated record.</returns>
    ValueTask<UserModel> CreateOrUpdateUser(UserModel user, CancellationToken ct);

    /// <summary>
    /// Deletes an user.
    /// </summary>
    /// <param name="id">Id of the user to be deleted.</param>
    /// <param name="ct">Cancellation Token.</param>
    ValueTask DeleteUser(Guid id, CancellationToken ct);

    /// <summary>
    /// Seeds the database with initial test users.
    /// </summary>
    /// <param name="hasher">Instance of password hasher.</param>
    /// <param name="logger">Instance of logger.</param>
    void SeedUsers(IPasswordHasher hasher, ILogger logger);
}
