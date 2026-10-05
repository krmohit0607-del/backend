using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Auth;
using MultiTenantSaaS.Application.Features.Auth.Commands;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    /// <summary>
    /// Authenticate a user with email and password, returning JWT access token and refresh token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await Mediator.Send(new LoginCommand(dto));
        return Ok(result);
    }

    /// <summary>
    /// Generate a new JWT access token and refresh token using an existing valid refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(dto));
        return Ok(result);
    }

    /// <summary>
    /// Revoke a refresh token and logout the user session.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? dto)
    {
        var result = await Mediator.Send(new RevokeTokenCommand(dto?.RefreshToken));
        return Ok(result);
    }
}
