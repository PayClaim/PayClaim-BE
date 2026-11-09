using Microsoft.EntityFrameworkCore;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.Repositories;

public class BenefitPlanRepository: Repository<BenefitPlan>, IBenefitPlanRepository
{
    public BenefitPlanRepository(PayClaimDbContext context)
        : base(context)
    {
    }

    public async Task<List<BenefitPlan>> GetByDepartmentAndPositionAsync(
        string departmentId,
        string positionId,
        CancellationToken ct = default)
    {
        var plans = await _context.BenefitPlans
            .Include(bp => bp.Benefit)
            .Where(bp =>
                (bp.DepartmentId == departmentId || bp.DepartmentId == "ALL") &&
                bp.PositionId == positionId)
            .ToListAsync(ct);

        return plans
            .GroupBy(bp => bp.BenefitId)
            .Select(g => g.OrderByDescending(bp => bp.DepartmentId == departmentId ? 1 : 0).First())
            .ToList();
    }
}