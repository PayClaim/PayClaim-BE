using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitPlans.Queries;

public record GetBenefitPlansQuery();

public class GetBenefitPlansHandler(PayClaimDbContext db)
{
    public async Task<List<BenefitPlanDto>> Handle( GetBenefitPlansQuery query, CancellationToken ct)
    {
        return await db.BenefitPlans
            .AsNoTracking()
            .OrderBy(bp => bp.DepartmentId)
            .ThenBy(bp => bp.PositionId)
            .Select(bp => new BenefitPlanDto
            {
                DepartmentId = bp.DepartmentId,
                PositionId = bp.PositionId,
                BenefitId = bp.BenefitId,
                LimitAmount = bp.LimitAmount
            })
            .ToListAsync(ct);
    }
}