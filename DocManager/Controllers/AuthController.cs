// Ignore Spelling: Auth

using DocManager.DTOs;
using DocManager.Exceptions;
using DocManager.Interfaces;
using DocManager.Utils;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocManager.Controllers;

/// <summary>
/// Controller responsible for user authentication.
/// </summary>
/// <param name="userService">Injected User Service instance.</param>
/// <param name="logger">Injected logger service instance.</param>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
public class AuthController(IUserService userService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ILogger _logger = logger;

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
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthOutputDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async ValueTask<IActionResult> Authenticate([FromBody] AuthInputDTO input, CancellationToken ct)
    {
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Attempting to authenticate `{email}`.", requestId, input.Email);
        var auth = await _userService.AuthenticateUser(input.Email ?? string.Empty, input.Password ?? string.Empty, ct);

        bool authenticated = auth is not null;
        _logger.LogInformation("[{requestId}] Authentication result for `{email}`: {result}", requestId, input.Email, authenticated ? "Authorized" : "Unauthorized");

        return authenticated ? Ok(auth) : Unauthorized(AUTH_ERROR_MESSAGE);
    }


    /// <summary>
    /// Gets information about the currently logged in user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An instance of <see cref="UserViewDTO"/>.</returns>
    /// <response code="200">If the user is authenticated, it returns an object with user details.</response>
    /// <response code="400">If a validation error occurs, it returns an object containing the error details.</response>
    /// <response code="401">If user is not properly authenticated.</response>
    /// <response code="403">If user does not have permission to execute the operation.</response>
    [HttpGet]
    [Authorize("User")]
    [ProducesResponseType(typeof(UserViewDTO), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> GetLoggedUserInfo(CancellationToken ct)
    {
        var userId = RequestContext.GetUserId(HttpContext);
        string userName = RequestContext.GetUserName(HttpContext);
        var requestId = RequestContext.GetRequestId(HttpContext);

        _logger.LogInformation("[{requestId}] Get Logged User Info => LoggedUserId: {LoggedUserId} || LoggedUserName: {LoggedUserName}",
            requestId, userId, userName);

        var user = await _userService.GetUser(userId, ct);
        return user is not null ? Ok(user) : throw new NotFoundException("User not found.");
    }
}
