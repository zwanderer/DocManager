using DocManager.DTOs;
using DocManager.Models;

namespace DocManager.Interfaces;

/// <summary>
/// Provides handling of business logic for <see cref="UserModel"/>.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Authenticates an user using email and password.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">User password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="AuthOutputDTO"/> containing authentication info if successful, or null otherwise.</returns>
    ValueTask<AuthOutputDTO?> AuthenticateUser(string email, string password, CancellationToken ct);

    /// <summary>
    /// Returns a single user.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/>.</returns>
    ValueTask<UserViewDTO?> GetUser(Guid id, CancellationToken ct);

    /// <summary>
    /// Returns a single user based on email.
    /// </summary>
    /// <param name="email">Email of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/>.</returns>
    ValueTask<UserViewDTO?> GetUserByEmail(string email, CancellationToken ct);

    /// <summary>
    /// Returns a list of all users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list containing instances of <see cref="UserModel"/>.</returns>
    ValueTask<IEnumerable<UserViewDTO>> GetUsers(CancellationToken ct);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">User data.</param>
    /// <param name="creator">Id of the logged user that create the new user.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/> with the data of the newly created user.</returns>
    ValueTask<UserViewDTO> CreateNewUser(CreateUserDTO user, Guid creator, CancellationToken ct);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="userId">Id of the user to be updated.</param>
    /// <param name="user">User data.</param>
    /// <param name="updater">Id of the logged user that update this user.s</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/> with the data of the updated user.</returns>
    ValueTask<UserViewDTO> UpdateUser(Guid userId, UpdateUserDTO user, Guid updater, CancellationToken ct);

    /// <summary>
    /// Deletes an user.
    /// </summary>
    /// <param name="userId">Id of the user to be deleted.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask DeleteUser(Guid userId, CancellationToken ct);
}