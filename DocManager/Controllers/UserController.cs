using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using DocManager.DTOs;
using DocManager.Interfaces;
using DocManager.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocManager.Controllers;

/// <summary>
/// Controller responsible for CRUD operations for model <see cref="UserModel"/>.
/// </summary>
/// <param name="userService">Injected User Service instance.</param>
/// <remarks>Requires the ADMIN role.</remarks>
/// <response code="400">If a validation error occurs, it returns an object containing the error details.</response>
/// <response code="401">If user is not properly authenticated.</response>
/// <response code="403">If user does not have permission to execute the operation.</response>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
[Authorize("Admin")]
public class UserController(IUserService userService) : ControllerBase
{

    private readonly IUserService _userService = userService;

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
        var users = await _userService.GetUsers(ct);
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
        var user = await _userService.GetUser(id, ct);
        return user is null ? NoContent() : Ok(user);
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
        string loggedUserId = HttpContext.User.FindFirstValue("id") ?? string.Empty;
        if (!Guid.TryParse(loggedUserId, out var creator))
            throw new Exception("Invalid logged user id.");

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
        string loggedUserId = HttpContext.User.FindFirstValue("id") ?? string.Empty;
        if (!Guid.TryParse(loggedUserId, out var updater))
            throw new Exception("Invalid logged user id.");

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
        string loggedUserId = HttpContext.User.FindFirstValue("id") ?? string.Empty;
        if (!Guid.TryParse(loggedUserId, out var deleter))
            throw new Exception("Invalid logged user id.");

        if (deleter == id)
            throw new Exception("An user cannot delete itself.");

        await _userService.DeleteUser(id, ct);
        return Ok();
    }
}
