using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WellData.Api.Auth;
using WellData.Api.Dtos;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(TokenService tokens) : ControllerBase
{
    /// <summary>Exchanges a user name and password for a bearer token.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> Token(TokenRequest request)
    {
        var token = tokens.Issue(request.UserName, request.Password);

        return token is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid user name or password.")
            : new TokenResponse(token.AccessToken, token.ExpiresAt, token.Role);
    }
}
