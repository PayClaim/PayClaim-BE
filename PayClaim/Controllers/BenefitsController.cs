using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Benefits.Commands;
using PayClaim.Features.Benefits.Queries;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BenefitsController(
    CreateBenefitHandler createHandler,
    GetBenefitsHandler getAllHandler,
    GetBenefitByIdHandler getByIdHandler,
    UpdateBenefitHandler updateHandler,
    DeleteBenefitHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<BenefitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BenefitDto>>> GetAll(CancellationToken ct)
    {
        var result = await getAllHandler.Handle(
            new GetBenefitsQuery(), ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BenefitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getByIdHandler.Handle(
            new GetBenefitByIdQuery(id), ct);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BenefitIdResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitIdResponseDto>> Create([FromBody] CreateBenefitDto request, CancellationToken ct)
    {
        var id = await createHandler.Handle(
            new CreateBenefitCommand(request), ct);

        return CreatedAtAction(nameof(GetById), new { id }, new BenefitIdResponseDto(id));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BenefitMessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitMessageResponseDto>> Update(Guid id, [FromBody] UpdateBenefitDto request, CancellationToken ct)
    {
        await updateHandler.Handle(
            new UpdateBenefitCommand(id, request), ct);

        return Ok(new BenefitMessageResponseDto("Benefit updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(BenefitMessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitMessageResponseDto>> Delete(Guid id, CancellationToken ct)
    {
        await deleteHandler.Handle(
            new DeleteBenefitCommand(id), ct);

        return Ok(new BenefitMessageResponseDto("Benefit deleted successfully."));
    }
}
