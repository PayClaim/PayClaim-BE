using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Benefits.Queries;

public record GetBenefitsQuery();

public class GetBenefitsHandler(PayClaimDbContext db)
{
    public async Task<List<BenefitDto>> Handle(GetBenefitsQuery query, CancellationToken ct)
    {
        return await db.Benefits
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BenefitDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                PeriodType = b.PeriodType,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name
            })
            .ToListAsync(ct);
    }
}