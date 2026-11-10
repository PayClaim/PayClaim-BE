using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitPlans.Queries;

public record GetBenefitPlanByIdQuery(string DepartmentId, Guid BenefitId, string PositionId) : IRequest<BenefitPlanDto>;

public class GetBenefitPlanByIdHandler(PayClaimDbContext db) : IRequestHandler<GetBenefitPlanByIdQuery, BenefitPlanDto>
{
    public async Task<BenefitPlanDto> Handle(GetBenefitPlanByIdQuery query, CancellationToken ct)
    {
        var benefitPlan = await db.BenefitPlans
            .AsNoTracking()
            .Where(bp =>
                bp.DepartmentId == query.DepartmentId &&
                bp.BenefitId == query.BenefitId &&
                bp.PositionId == query.PositionId)
            .Select(bp => new BenefitPlanDto
            {
                DepartmentId = bp.DepartmentId,
                PositionId = bp.PositionId,
                BenefitId = bp.BenefitId,
                LimitAmount = bp.LimitAmount
            })
            .FirstOrDefaultAsync(ct);

        if (benefitPlan == null)
            throw new NotFoundException("Benefit plan not found.");

        return benefitPlan;
    }
}
