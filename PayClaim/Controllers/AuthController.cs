using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Auth;
using PayClaim.DTOs.Common;
using PayClaim.Features.Auth.Commands.ChangePassword;
using PayClaim.Features.Auth.Commands.Login;
using PayClaim.Features.Auth.Commands.ResetPassword;
using PayClaim.Features.Auth.Queries.GetMe;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    LoginHandler loginHandler,
    GetMeHandler getMeHandler,
    ChangePasswordHandler changePasswordHandler,
    ResetPasswordHandler resetPasswordHandler) : ControllerBase
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
    public async Task<ActionResult<ApiResponse<UserProfileResponseDto>>> GetMe(CancellationToken ct)
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

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken ct)
    {
        var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("AccountId")
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing user identity in token.", StatusCodes.Status401Unauthorized));
        }

        await changePasswordHandler.Handle(new ChangePasswordCommand(accountId, request), ct);
        return Ok(ApiResponse.Ok("Password changed successfully."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
    {
        await resetPasswordHandler.Handle(new ResetPasswordCommand(request), ct);
        return Ok(ApiResponse.Ok("Password has been reset successfully."));
    }
}
