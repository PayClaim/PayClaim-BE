using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.EmployeeBenefit;
using PayClaim.DTOs.Master;
using PayClaim.Features.EmployeeBenefits.Commands;
using PayClaim.Features.EmployeeBenefits.Queries;
using PayClaim.Features.EmployeeBenefits.Queries.GetMyBenefits;
using PayClaim.Features.EmployeeBenefits.Query.GetEmployeeBenefits;
using System.Security.Claims;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeBenefitsController(
    GetEmployeeBenefitsHandler getAllHandler,
    GetMyBenefitsHandler getMyHandler,
    AdjustEmployeeBenefitHandler adjustHandler) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MyBenefitSummaryDto>>> GetAll(
        [FromQuery] EmployeeBenefitFilterQuery filter,
        CancellationToken ct)
    {
        var result = await getAllHandler.Handle(
            new GetEmployeeBenefitsQuery(filter), ct);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MyBenefitSummaryDto>> GetMyBenefits(CancellationToken ct)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var result = await getMyHandler.Handle(
            new GetMyBenefitsQuery(employeeId), ct);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Adjust(
        Guid id,
        [FromBody] AdjustEmployeeBenefitDto request,
        CancellationToken ct)
    {
        await adjustHandler.Handle(
            new AdjustEmployeeBenefitCommand(id, request), ct);

        return Ok(new { message = "Employee benefit adjusted successfully." });
    }
}