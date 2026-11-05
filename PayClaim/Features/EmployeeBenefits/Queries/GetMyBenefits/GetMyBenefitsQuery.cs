using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.EmployeeBenefit;

namespace PayClaim.Features.EmployeeBenefits.Queries.GetMyBenefits;

public record GetMyBenefitsQuery(Guid EmployeeId);

public class GetMyBenefitsHandler(PayClaimDbContext db)
{
    public async Task<List<MyBenefitSummaryDto>> Handle(
        GetMyBenefitsQuery query,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await db.EmployeeBenefits
            .AsNoTracking()
            .Where(eb => eb.EmployeeId == query.EmployeeId)
            .OrderBy(eb => eb.Benefit.Name)
            .Select(eb => new MyBenefitSummaryDto
            {
                EmployeeBenefitId = eb.Id,
                BenefitId = eb.BenefitId,
                BenefitName = eb.Benefit.Name,
                CategoryName = eb.Benefit.Category.Name,
                PeriodType = eb.Benefit.PeriodType,
                Balance = eb.Balance,
                StartDate = eb.StartDate,
                EndDate = eb.EndDate,
                IsActive = eb.StartDate <= today && eb.EndDate >= today
            })
            .ToListAsync(ct);
    }
}