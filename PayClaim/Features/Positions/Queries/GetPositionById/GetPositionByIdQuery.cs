using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Positions.Queries.GetPositionById;

public record GetPositionByIdQuery(string Id) : IRequest<PositionDto>;

public class GetPositionByIdQueryHandler(PayClaimDbContext db) : IRequestHandler<GetPositionByIdQuery, PositionDto>
{
    public async Task<PositionDto> Handle(
        GetPositionByIdQuery query,
        CancellationToken ct)
    {
        var position = await db.Positions
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Title = p.Title
            })
            .FirstOrDefaultAsync(ct);

        if (position == null)
        {
            throw new NotFoundException("Position not found.");
        }

        return position;
    }
}
