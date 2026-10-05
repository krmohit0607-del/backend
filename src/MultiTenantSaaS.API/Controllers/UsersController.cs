using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Auth;
using MultiTenantSaaS.Application.Features.Auth.Queries;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController : BaseApiController
{
    /// <summary>
    /// Get the profile details of the currently logged-in user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var result = await Mediator.Send(new GetCurrentUserQuery());
        return Ok(result);
    }
}
