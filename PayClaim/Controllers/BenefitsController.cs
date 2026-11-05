using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Benefits.Commands;
using PayClaim.Features.Benefits.Queries;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitsController(CreateBenefitHandler createHandler, GetBenefitsHandler getAllHandler, GetBenefitByIdHandler getByIdHandler,
    UpdateBenefitHandler updateHandler, DeleteBenefitHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BenefitDto>>> GetAll(CancellationToken ct)
    {
        var result = await getAllHandler.Handle(
            new GetBenefitsQuery(), ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BenefitDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getByIdHandler.Handle(
            new GetBenefitByIdQuery(id), ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBenefitDto request, CancellationToken ct)
    {
        var id = await createHandler.Handle(
            new CreateBenefitCommand(request), ct);

        return Created($"/api/benefits/{id}", new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBenefitDto request, CancellationToken ct)
    {
        await updateHandler.Handle(
            new UpdateBenefitCommand(id, request), ct);

        return Ok(new { message = "Benefit updated successfully." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await deleteHandler.Handle(
            new DeleteBenefitCommand(id), ct);

        return Ok(new { message = "Benefit deleted successfully." });
    }
}
