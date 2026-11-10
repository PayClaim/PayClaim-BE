using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.EmployeeBenefit;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.EmployeeBenefits.Query.GetEmployeeBenefits;

public record GetEmployeeBenefitsQuery(EmployeeBenefitFilterQuery Filter) : IRequest<List<MyBenefitSummaryDto>>;

public class GetEmployeeBenefitsHandler(PayClaimDbContext db) : IRequestHandler<GetEmployeeBenefitsQuery, List<MyBenefitSummaryDto>>
{
    public async Task<List<MyBenefitSummaryDto>> Handle(GetEmployeeBenefitsQuery query, CancellationToken ct)
    {
        var employeeBenefits = db.EmployeeBenefits
            .AsNoTracking()
            .AsQueryable();

        if (query.Filter.EmployeeId.HasValue)
        {
            employeeBenefits = employeeBenefits.Where(eb => eb.EmployeeId == query.Filter.EmployeeId.Value);
        }
        if (query.Filter.BenefitId.HasValue)
        {
            employeeBenefits = employeeBenefits.Where(eb => eb.BenefitId == query.Filter.BenefitId.Value);
        }
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await employeeBenefits
            .OrderBy(eb => eb.Employee.FullName)
            .ThenBy(eb => eb.Benefit.Name)
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
                IsActive = eb.StartDate <= today && eb.EndDate >= today,
                EmployeeName = eb.Employee.FullName,
            })
            .ToListAsync(ct);
    }
}
