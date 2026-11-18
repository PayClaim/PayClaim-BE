using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.EmployeeBenefit;
using PayClaim.DTOs.Master;
using PayClaim.Features.EmployeeBenefits.Queries.GetMyBenefits;
using PayClaim.Features.EmployeeBenefits.Query.GetEmployeeBenefits;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeBenefitsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MyBenefitSummaryDto>>> GetAll(
        [FromQuery] EmployeeBenefitFilterQuery filter,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployeeBenefitsQuery(filter), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<List<MyBenefitSummaryDto>>> GetMyBenefits(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyBenefitsQuery(), ct);
        return Ok(result);
    }
}
