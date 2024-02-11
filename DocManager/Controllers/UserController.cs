using System.ComponentModel.DataAnnotations;

using DocManager.DTOs;
using DocManager.Interfaces;
using DocManager.Models;
using DocManager.Utils;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocManager.Controllers;

/// <summary>
/// Controller responsible for CRUD operations for model <see cref="UserModel"/>.
/// </summary>
/// <param name="userService">Injected User Service instance.</param>
/// <param name="logger">Injected logger service instance.</param>
/// <remarks>Requires the ADMIN role.</remarks>
/// <response code="400">If a validation error occurs, it returns an object containing the error details.</response>
/// <response code="401">If user is not properly authenticated.</response>
/// <response code="403">If user does not have permission to execute the operation.</response>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
[Authorize("Admin")]
public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
{

    private readonly IUserService _userService = userService;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Gets a list of all users stored.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="UserViewDTO"/> instances.</returns>
    /// <response code="200">If the operation is successful, it returns a list with all users.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserViewDTO>), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var userId = RequestContext.GetUserId(HttpContext);
        string userName = RequestContext.GetUserName(HttpContext);
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Get All Users => LoggedUserId: {LoggedUserId} || LoggedUserName: {LoggedUserName}",
            requestId, userId, userName);

        var users = await _userService.GetUsers(ct);

        _logger.LogInformation("[{requestId}] Returned {count} users.", requestId, users?.Count());
        return Ok(users);
    }

    /// <summary>
    /// Gets a user by its Id.
    /// </summary>
    /// <param name="id" example="1f15eaaa-fc2f-4b3c-9dde-0ffe6ca9b709">Id of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/>.</returns>
    /// <response code="200">If the user is found, it returns an object with user details.</response>
    /// <response code="204">If the user is NOT found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserViewDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async ValueTask<IActionResult> GetUser([Required][FromRoute] Guid id, CancellationToken ct)
    {
        var userId = RequestContext.GetUserId(HttpContext);
        string userName = RequestContext.GetUserName(HttpContext);
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Get All Users => UserId: {userId} || LoggedUserId: {LoggedUserId} || LoggedUserName: {LoggedUserName}",
            requestId, id, userId, userName);

        var user = await _userService.GetUser(id, ct);

        if (user is null)
        {
            _logger.LogInformation("[{requestId}] User `{userId}` not found.", requestId, id);
            return NoContent();
        }
        else
        {
            _logger.LogInformation("[{requestId}] User `{userId}` named `{username}` found.", requestId, id, user.Email);
            return Ok(user);
        }
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">Information for the new user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/>.</returns>
    /// <response code="200">If the operation is successful, it returns an object with the new user details.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserViewDTO), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> CreateUser([FromBody] CreateUserDTO user, CancellationToken ct)
    {
        var creator = RequestContext.GetUserId(HttpContext);
        string userName = RequestContext.GetUserName(HttpContext);
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Create User => Name: `{name}` || Email: {email} || Roles: {roles} || " +
                               "LoggedUserId: {LoggedUserId} || LoggedUserName: {LoggedUserName}",
            requestId, user.Name, user.Email, string.Join(',', user.RolesAssigned), creator, userName);
        var newUser = await _userService.CreateNewUser(user, creator, ct);
        return Ok(newUser);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id" example="1f15eaaa-fc2f-4b3c-9dde-0ffe6ca9b709">Id of the user to be updated.</param>
    /// <param name="user">Information for the existing user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/>.</returns>
    /// <response code="200">If the operation is successful, it returns an object with the existing user details.</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(UserViewDTO), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> UpdateUser([Required][FromRoute] Guid id, [FromBody] UpdateUserDTO user, CancellationToken ct)
    {
        var updater = RequestContext.GetUserId(HttpContext);
        string userName = RequestContext.GetUserName(HttpContext);
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Update User => UserId: `{userId}` || LoggedUserId: {LoggedUserId} || LoggedUserName: {LoggedUserName}",
            requestId, id, updater, userName);

        var newUser = await _userService.UpdateUser(id, user, updater, ct);
        return Ok(newUser);
    }

    /// <summary>
    /// Deletes an existing user.
    /// </summary>
    /// <param name="id" example="2e88699b-a81d-4966-97f4-2cd5898ee9b2">Id of the user to be deleted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">If the operation is successful.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> DeleteUser([Required][FromRoute] Guid id, CancellationToken ct)
    {
        var deleter = RequestContext.GetUserId(HttpContext);
        string userName = RequestContext.GetUserName(HttpContext);
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Delete User => UserId: `{userId}` || LoggedUserId: {LoggedUserId} || LoggedUserName: {LoggedUserName}",
            requestId, id, deleter, userName);

        if (deleter == id)
            throw new InvalidOperationException("An user cannot delete itself.");

        await _userService.DeleteUser(id, ct);
        return Ok();
    }
}
