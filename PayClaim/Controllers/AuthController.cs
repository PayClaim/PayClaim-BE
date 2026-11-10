using MediatR;
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
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request), ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponseDto>> GetMe(CancellationToken ct)
    {
        var profile = await sender.Send(new GetMeQuery(), ct);
        return Ok(profile);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken ct)
    {
        await sender.Send(new ChangePasswordCommand(request), ct);
        return Ok(ApiResponse.Ok("Password changed successfully."));
    }

    [HttpPost("reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
    {
        await sender.Send(new ResetPasswordCommand(request), ct);
        return Ok(ApiResponse.Ok("Password has been reset successfully."));
    }
}
