using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Positions.Commands.CreatePosition;
using PayClaim.Features.Positions.Commands.DeletePosition;
using PayClaim.Features.Positions.Commands.UpdatePosition;
using PayClaim.Features.Positions.Queries.GetPositionById;
using PayClaim.Features.Positions.Queries.GetPositions;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController(
    CreatePositionHandler createPositionHandler,GetPositionsHandler getPositionsHandler, GetPositionByIdQueryHandler getPositionByIdHandler,
    UpdatePositionHandler updatePositionHandler, DeletePositionHandler deletePositionHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var positions = await getPositionsHandler.Handle(
            new GetPositionsQuery(), ct);

        return Ok(positions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var position = await getPositionByIdHandler.Handle(
            new GetPositionByIdQuery(id), ct);

        return Ok(position);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePositionDto request, CancellationToken ct)
    {
        var id = await createPositionHandler.Handle(
            new CreatePositionCommand(request), ct);

        return Created($"/api/positions/{id}", new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePositionDto request, CancellationToken ct)
    {
        await updatePositionHandler.Handle(
            new UpdatePositionCommand(id, request), ct);

        return Ok("Position updated successfully.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete( string id, CancellationToken ct)
    {
        await deletePositionHandler.Handle(
            new DeletePositionCommand(id), ct);

        return Ok("Position deleted successfully.");
    }
}