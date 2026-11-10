using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitPlans.Queries;

public record GetBenefitPlansQuery : IRequest<List<BenefitPlanDto>>;

public class GetBenefitPlansHandler(PayClaimDbContext db) : IRequestHandler<GetBenefitPlansQuery, List<BenefitPlanDto>>
{
    public async Task<List<BenefitPlanDto>> Handle(GetBenefitPlansQuery query, CancellationToken ct)
    {
        return await db.BenefitPlans
            .AsNoTracking()
            .OrderBy(bp => bp.DepartmentId)
            .ThenBy(bp => bp.PositionId)
            .Select(bp => new BenefitPlanDto
            {
                DepartmentId = bp.DepartmentId,
                DepartmentName = bp.Department.Name,

                PositionId = bp.PositionId,
                PositionTitle = bp.Position.Title,

                BenefitId = bp.BenefitId,
                BenefitName = bp.Benefit.Name,

                LimitAmount = bp.LimitAmount
            })
            .ToListAsync(ct);
    }
}
