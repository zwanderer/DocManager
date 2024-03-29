﻿using DocManager.DTOs;
using DocManager.Interfaces;
using DocManager.Models;

namespace DocManager.Services;

/// <inheritdoc cref="IUserService"/>
/// <param name="repository">Injected instance of repository.</param>
/// <param name="hasher">Injected instance of password hasher.</param>
/// <param name="tokenGenerator">Inject instance of token generator.</param>
public class UserService(IUserRepository repository, IPasswordHasher hasher, ITokenGenerator tokenGenerator) : IUserService
{
    private readonly IUserRepository _repository = repository;
    private readonly IPasswordHasher _hasher = hasher;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;

    /// <inheritdoc/>
    public async ValueTask<AuthOutputDTO?> AuthenticateUser(string email, string password, CancellationToken ct)
    {
        var userModel = await _repository.GetUserByEmail(email, ct);
        if (userModel is null)
            return null;

        string hash = _hasher.HashPassword(email, password);

        if (!string.Equals(hash, userModel.PasswordHash))
            return null;

        var output = _tokenGenerator.GenerateToken(userModel);
        return output;
    }

    private static UserViewDTO ToDTO(UserModel model, Dictionary<Guid, UserModel> allUsers)
    {
        var dto = new UserViewDTO
        {
            UserId = model.UserId,
            Name = model.Name,
            Email = model.Email,
            CreateTS = model.CreateTS,
            UpdateTS = model.UpdateTS,
            RolesAssigned = model.RolesAssigned
        };

        if (allUsers.TryGetValue(model.CreatedByUserId, out var creator))
            dto.CreatedBy = creator.Email;

        if (model.UpdatedByUserID is not null && allUsers.TryGetValue(model.UpdatedByUserID.Value, out var updater))
            dto.UpdatedBy = updater.Email;

        return dto;
    }

    private async ValueTask<Dictionary<Guid, UserModel>> GetCreatorUpdater(UserModel user, CancellationToken ct)
    {
        var allUsers = new Dictionary<Guid, UserModel>();

        var creator = await _repository.GetUser(user.CreatedByUserId, ct);
        if (creator is not null)
            allUsers.TryAdd(creator.UserId, creator);

        if (user.UpdatedByUserID is not null)
        {
            var updater = await _repository.GetUser(user.UpdatedByUserID.Value, ct);
            if (updater is not null)
                allUsers.TryAdd(updater.UserId, updater);
        }

        return allUsers;
    }

    /// <inheritdoc/>
    public async ValueTask<UserViewDTO?> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _repository.GetUser(id, ct);
        if (user is not null)
        {
            var allUsers = await GetCreatorUpdater(user, ct);

            return ToDTO(user, allUsers);
        }

        return null;
    }

    /// <inheritdoc/>
    public async ValueTask<UserViewDTO?> GetUserByEmail(string email, CancellationToken ct)
    {
        var user = await _repository.GetUserByEmail(email, ct);
        if (user is not null)
        {
            var allUsers = await GetCreatorUpdater(user, ct);

            return ToDTO(user, allUsers);
        }

        return null;
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<UserViewDTO>> GetUsers(CancellationToken ct)
    {
        var allUsers = await _repository.GetUsers(ct) ?? new List<UserModel>();
        return allUsers.Select(u => ToDTO(u, allUsers.ToDictionary(u => u.UserId)));
    }

    /// <inheritdoc/>
    public async ValueTask<UserViewDTO> CreateNewUser(CreateUserDTO user, Guid creator, CancellationToken ct)
    {
        if (!(user.RolesAssigned?.Count > 0))
            throw new Exception("At least one role should be assigned.");

        string hash = _hasher.HashPassword(user.Email, user.Password);

        var userModel = new UserModel
        {
            UserId = Guid.NewGuid(),
            Name = user.Name,
            Email = user.Email.ToLower(),
            RolesAssigned = user.RolesAssigned,
            CreatedByUserId = creator,
            CreateTS = DateTimeOffset.UtcNow,
            PasswordHash = hash,
            UpdatedByUserID = null,
            UpdateTS = null
        };

        userModel = await _repository.CreateOrUpdateUser(userModel, ct);
        var creatorUser = await _repository.GetUser(creator, ct);
        return ToDTO(userModel, new Dictionary<Guid, UserModel> { { creator, creatorUser } });
    }

    /// <inheritdoc/>
    public async ValueTask<UserViewDTO> UpdateUser(Guid userId, UpdateUserDTO user, Guid updater, CancellationToken ct)
    {
        var existingUser = await _repository.GetUser(userId, ct) ?? throw new Exception("User not found.");
        bool modified = false;

        if (user.Password is not null)
        {
            string hash = _hasher.HashPassword(existingUser.Email, user.Password);
            existingUser.PasswordHash = hash;
            modified = true;
        }

        if (user.Name is not null)
        {
            existingUser.Name = user.Name;
            modified = true;
        }

        if (user.RolesAssigned?.Count > 0)
        {
            existingUser.RolesAssigned = user.RolesAssigned;
            modified = true;
        }

        if (modified)
        {
            existingUser.UpdatedByUserID = updater;
            existingUser.UpdateTS = DateTimeOffset.UtcNow;
            existingUser = await _repository.CreateOrUpdateUser(existingUser, ct);
        }

        var allUsers = await GetCreatorUpdater(existingUser, ct);
        return ToDTO(existingUser, allUsers);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteUser(Guid userId, CancellationToken ct)
    {
        var existingUser = await _repository.GetUser(userId, ct) ?? throw new Exception("User not found.");
        await _repository.DeleteUser(existingUser.UserId, ct);
    }
}
