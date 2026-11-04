using PayClaim.Entities;

namespace PayClaim.Data.Repositories.Interfaces;

public interface IBenefitPlanRepository : IRepository<BenefitPlan>
{
    Task<List<BenefitPlan>> GetByDepartmentAndPositionAsync(
        string departmentId,
        string positionId,
        CancellationToken ct = default);
}