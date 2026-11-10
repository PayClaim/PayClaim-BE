using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.BenefitPlans.Commands;
using PayClaim.Features.BenefitPlans.Queries;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitPlansController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<BenefitPlanDto>>> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetBenefitPlansQuery(), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{departmentId}/{benefitId:guid}/{positionId}")]
    public async Task<ActionResult<BenefitPlanDto>> GetById(string departmentId, Guid benefitId, string positionId, CancellationToken ct)
    {
        var result = await sender.Send(new GetBenefitPlanByIdQuery(departmentId, benefitId, positionId), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertBenefitPlanDto request, CancellationToken ct)
    {
        await sender.Send(new CreateBenefitPlanCommand(request), ct);
        return Ok(new { message = "Benefit plan created successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{departmentId}/{benefitId:guid}/{positionId}")]
    public async Task<IActionResult> Update(
        string departmentId,
        Guid benefitId,
        string positionId, 
        [FromBody] UpsertBenefitPlanDto request,
        CancellationToken ct)
    {
        await sender.Send(new UpdateBenefitPlanCommand(departmentId, benefitId, positionId, request), ct);
        return Ok(new { message = "Benefit plan updated successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{departmentId}/{benefitId:guid}/{positionId}")]
    public async Task<IActionResult> Delete(string departmentId, Guid benefitId, string positionId, CancellationToken ct)
    {
        await sender.Send(new DeleteBenefitPlanCommand(departmentId, benefitId, positionId), ct);
        return Ok(new { message = "Benefit plan deleted successfully." });
    }
}
