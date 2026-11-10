using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Positions.Queries.GetPositions;

public record GetPositionsQuery : IRequest<IReadOnlyCollection<PositionDto>>;

public class GetPositionsHandler(PayClaimDbContext db) : IRequestHandler<GetPositionsQuery, IReadOnlyCollection<PositionDto>>
{
    public async Task<IReadOnlyCollection<PositionDto>> Handle(
        GetPositionsQuery query,
        CancellationToken ct)
        => await db.Positions
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Title = p.Title
            })
            .ToListAsync(ct);
}
