// Ignore Spelling: Auth

using DocManager.DTOs;
using DocManager.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocManager.Controllers;

/// <summary>
/// Controller responsible for user authentication.
/// </summary>
/// <param name="userService">Injected User Service instance.</param>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
[AllowAnonymous]
public class AuthController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Error message when an authentication attempt fails.
    /// </summary>
    public const string AUTH_ERROR_MESSAGE = "Invalid email or password.";

    /// <summary>
    /// Authenticates the user using his email and password.
    /// </summary>
    /// <param name="input">Payload containing the user's email and password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object containing a token to be used in further calls.</returns>
    /// <response code="200">If authentication is successful, it returns an object containing the token for further API calls.</response>
    /// <response code="400">If a validation error occurs, it returns an object containing the error details.</response>
    /// <response code="401">If authentication is NOT successful.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AuthOutputDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async ValueTask<IActionResult> Authenticate([FromBody] AuthInputDTO input, CancellationToken ct)
    {
        var auth = await _userService.AuthenticateUser(input.Email ?? string.Empty, input.Password ?? string.Empty, ct);

        return auth is null ? Unauthorized(AUTH_ERROR_MESSAGE) : Ok(auth);
    }
}
