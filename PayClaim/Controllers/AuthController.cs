using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Auth;
using PayClaim.DTOs.Common;
using PayClaim.Features.Auth.Commands.Login;
using PayClaim.Features.Auth.Queries.GetMe;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    LoginHandler loginHandler,
    GetMeHandler getMeHandler) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await loginHandler.Handle(new LoginCommand(request), ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("AccountId")
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing user identity in token.", StatusCodes.Status401Unauthorized));
        }

        var profile = await getMeHandler.Handle(new GetMeQuery(accountId), ct);
        return Ok(ApiResponse<UserProfileResponseDto>.Ok(profile, "User profile retrieved successfully."));
    }
}
