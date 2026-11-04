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
        return await _context.BenefitPlans
            .Include(bp => bp.Benefit)
            .Where(bp =>
                bp.DepartmentId == departmentId &&
                bp.PositionId == positionId)
            .ToListAsync(ct);
    }
}