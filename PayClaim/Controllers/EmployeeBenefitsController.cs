using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.EmployeeBenefit;
using PayClaim.DTOs.Master;
using PayClaim.Features.EmployeeBenefits.Commands;
using PayClaim.Features.EmployeeBenefits.Queries;
using PayClaim.Features.EmployeeBenefits.Queries.GetMyBenefits;
using PayClaim.Features.EmployeeBenefits.Query.GetEmployeeBenefits;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeBenefitsController(
    GetEmployeeBenefitsHandler getAllHandler,
    GetMyBenefitsHandler getMyHandler,
    AdjustEmployeeBenefitHandler adjustHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MyBenefitSummaryDto>>> GetAll(
        [FromQuery] EmployeeBenefitFilterQuery filter,
        CancellationToken ct)
    {
        var result = await getAllHandler.Handle(
            new GetEmployeeBenefitsQuery(filter), ct);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MyBenefitSummaryDto>> GetMyBenefits(Guid id, CancellationToken ct)
    {
        var result = await getMyHandler.Handle(
            new GetMyBenefitsQuery(id), ct);

        return Ok(result);
    }

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