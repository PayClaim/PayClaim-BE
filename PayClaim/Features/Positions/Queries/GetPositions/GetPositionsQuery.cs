using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Positions.Queries.GetPositions;

public record GetPositionsQuery : IRequest<IReadOnlyCollection<PositionDto>>;

public class GetPositionsHandler(PayClaimDbContext db, IMemoryCache cache)
    : IRequestHandler<GetPositionsQuery, IReadOnlyCollection<PositionDto>>
{
    public const string CacheKey = "master:positions";

    public async Task<IReadOnlyCollection<PositionDto>> Handle(
        GetPositionsQuery query,
        CancellationToken ct)
        => (await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await db.Positions
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new PositionDto
                {
                    Id = p.Id,
                    Title = p.Title
                })
                .ToListAsync(ct);
        }))!;
}
