using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitPlans.Queries;

public record GetBenefitPlansQuery(BenefitPlanFilterQuery Filter) : IRequest<PagedResponse<BenefitPlanDto>>;

public class GetBenefitPlansHandler(PayClaimDbContext db) : IRequestHandler<GetBenefitPlansQuery, PagedResponse<BenefitPlanDto>>
{
    public async Task<PagedResponse<BenefitPlanDto>> Handle(GetBenefitPlansQuery query, CancellationToken ct)
    {
        var filter = query.Filter;
        var dbQuery = db.BenefitPlans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.DepartmentId))
            dbQuery = dbQuery.Where(bp => bp.DepartmentId == filter.DepartmentId);

        if (!string.IsNullOrWhiteSpace(filter.PositionId))
            dbQuery = dbQuery.Where(bp => bp.PositionId == filter.PositionId);

        if (filter.BenefitId.HasValue)
            dbQuery = dbQuery.Where(bp => bp.BenefitId == filter.BenefitId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            dbQuery = dbQuery.Where(bp =>
                bp.DepartmentId.Contains(s) ||
                bp.Department.Name.Contains(s) ||
                bp.PositionId.Contains(s) ||
                bp.Position.Title.Contains(s) ||
                bp.Benefit.Name.Contains(s));
        }

        var totalCount = await dbQuery.CountAsync(ct);

        var orderedQuery = dbQuery
            .OrderBy(bp => bp.DepartmentId)
            .ThenBy(bp => bp.PositionId)
            .ThenBy(bp => bp.BenefitId);

        int pageNumber = 1;
        int pageSize = totalCount > 0 ? totalCount : 1;

        IQueryable<PayClaim.Entities.BenefitPlan> pagedQuery;
        if (filter.PageNumber.HasValue || filter.PageSize.HasValue)
        {
            pageNumber = filter.PageNumber.GetValueOrDefault(1);
            if (pageNumber < 1) pageNumber = 1;

            pageSize = filter.PageSize.GetValueOrDefault(10);
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            pagedQuery = orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
        else
        {
            pagedQuery = orderedQuery;
        }

        var items = await pagedQuery
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

        return new PagedResponse<BenefitPlanDto>(items, totalCount, pageNumber, pageSize);
    }
}
