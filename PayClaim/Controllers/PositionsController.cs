using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Positions.Queries.GetPositionById;
using PayClaim.Features.Positions.Queries.GetPositions;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController(
    GetPositionsHandler getPositionsHandler,
    GetPositionByIdQueryHandler getPositionByIdHandler) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PositionDto>>> GetAll(CancellationToken ct)
    {
        var positions = await getPositionsHandler.Handle(
            new GetPositionsQuery(), ct);

        return Ok(positions);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<PositionDto>> GetById(string id, CancellationToken ct)
    {
        var position = await getPositionByIdHandler.Handle(
            new GetPositionByIdQuery(id), ct);

        return Ok(position);
    }
}
