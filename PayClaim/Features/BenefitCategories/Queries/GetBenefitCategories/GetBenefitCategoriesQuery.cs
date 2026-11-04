using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitCategories.Queries.GetBenefitCategories;

public record GetBenefitCategoriesQuery;

public class GetBenefitCategoriesHandler(PayClaimDbContext db)
{
    public async Task<IReadOnlyCollection<BenefitCategoryDto>> Handle(
        GetBenefitCategoriesQuery query,
        CancellationToken ct)
        => await db.BenefitCategories
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new BenefitCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(ct);
}