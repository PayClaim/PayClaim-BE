using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Benefits.Commands;
using PayClaim.Features.Benefits.Queries;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<BenefitDto>>> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetBenefitsQuery(), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BenefitDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetBenefitByIdQuery(id), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBenefitDto request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateBenefitCommand(request), ct);
        return Created($"/api/benefits/{id}", new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBenefitDto request, CancellationToken ct)
    {
        await sender.Send(new UpdateBenefitCommand(id, request), ct);
        return Ok(new { message = "Benefit updated successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteBenefitCommand(id), ct);
        return Ok(new { message = "Benefit deleted successfully." });
    }
}
